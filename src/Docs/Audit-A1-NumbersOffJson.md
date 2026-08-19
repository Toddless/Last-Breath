# Аудит А-1 «числа мимо json» (2026-08-19, read-only; фиксы — отдельными задачами)

Свод по 25 активным способностям каталога: каждое ли несущее число приходит из данных. Сборки не запускались — выводы чтением кода, строки по состоянию дерева на дату аудита.

## Механизм (доказан по коду)

`BaseAbilityData.json` (25 abilities + 96 augments) → DTO `Core/Data/AbilityData/AbilityBaseData.cs:24` → `AbilityProvider.cs:74-79` → фабрики `AbilityProvider.Factories.cs:15-42`. База `Ability.cs:357-364` регистрирует Cooldown/CostValue/CostType + ВСЕ `abilityProperties` (camelCase→PascalCase, `Ability.cs:429`). Наследники добавляют дефолты ПОСЛЕ `base` через `RegisterDefault`/`RegisterAppliedDuration` = `TryAdd` (`AbilityParameterSet.cs:82-85, 93-97`) — **молчаливый no-op на занятом ключе**. Правило: **json всегда побеждает дефолт кода**; дефолт живёт только при отсутствии ключа, без Tracker-записи.

Края: (1) `RegisterDefault` ДО `base` заставил бы json падать с TrackError — сегодня все 25 зовут `base` первой строкой, капкан не взведён, но это дисциплина, не гард. (2) Чтение незарегистрированного ключа = TrackNotFound + 0f (`AbilityParameterSet.cs:177-188`); `ValueOr` (`:204`) — тихий путь. Обратное направление («ключ json без читателя») закрыто гардом `AbilityDataKeyTests.cs:36`; направление аудита («число в коде без json») не покрыто ничем.

## Конфликты и код-онли (классы: а-дрейф = json побеждает, дефолт в коде лжёт; б = json есть, код игнорирует; в = числа в json нет вовсе)

| Способность | Ключ | Код (file:line) | В json? | Побеждает | Класс |
|---|---|---|---|---|---|
| Критический расчет | крит-шанс бафа | `BaseCriticalChance = 0.15f` — `CriticalCalculation.cs:19`, применён `:36` | нет | код | **в** |
| Критический расчет | `Duration` | `RegisterDefault(Duration, 1)` — `:42` | `duration: 3` | **json (3)** | а-дрейф |
| Критический расчет | `AdditionalDurationAmount` | `Extend(1)` — `CritCalculationBuff.cs:36` | `additionalDurationAmount: 1` | **код** (json никем не читается; в белом списке `AbilityDataKeyTests.cs:25-33`) | **б** |
| Банка яда | `PoisonDuration` | `RegisterAppliedDuration(PoisonDuration, 3)` — `JarOfPoison.cs:27` | нет (`abilityProperties: {}`) | код | **в** |
| Банка яда | доля крови в тике | `DefaultPercentFromDamage = 0.7f` — `DamageOverTurnEffect.cs:21`, подстановка `:36`; вызов без параметра `JarOfPoison.cs:62` | канон `Effect_Damage_Over_Turn_Poison.percentFromDamage = 0.35` | **код (0.7)** | **б** + ноль-дефолт |
| Помощь близнеца (атака) | доля в тике горения | тот же 0.7f; вызов `TwinAssistAttack.cs:66` | канон 0.45 | **код (0.7)** | **б** + ноль-дефолт |
| Берсерк | шанс серии | `MinContinueChance 0.05 / MaxContinueChance 0.80` — `BerserkFury.cs:23-24`, `:83` | нет | код | **в** |
| Армагеддон | шаг за недостающее HP | `MissingHpStep = 5f` — `Armageddon.cs:25`, `:103` | нет | код | **в** |
| Армагеддон | `Stage3HpCost` | `RegisterDefault(0.30f)` — `:86` | `stage3HpCost: 0.5` | **json (0.5)** | а-дрейф |
| Ледяная глыба | стан стадии 2 | `plan.StunDuration += 1` — `IceBlocks.cs:114` | нет | код | **в** |
| Ледяная глыба | ×2 за съеденный стан | `TryConsumeStun ? 2f : 1f` — `:78` | нет (у аугмента `upgradeProperties: {}`) | код | **в** |
| Цепная молния | прыжок стадии 2 | `plan.Jumps += 1` — `ChainLightning.cs:83` | нет | код | **в** |
| Глубокая заморозка | `HealReductionStacks` / `ShredStacks` | `RegisterDefault(2)` / `(1)` — `DeepFreeze.cs:68-69` | нет | код | **в** ×2 |
| Серия атак | обрыв по уклонению | `window.OwnerAttacks < 2` — `SoAsDefaultExecutionStrategy.cs:45` | нет | код | **в** |
| Осколки льда | 6 ключей Shrapnel/SecondStage | `IceShards.cs:49-54` (50/0.15/0.55/120/0.35/1.2) | 30/0.05/0.35/100/0.25/1.15 | **json** все шесть | а-дрейф ×6 |
| Серия атак | `DamageMultiplier` | `RegisterDefault(1.3f)` — `SeriesOfAttacks.cs:31` | `damageMultiplier: 1` | **json (1)** | а-дрейф |
| Взрыв яда | `ExecutionThreshold` | `RegisterDefault(42)` — `PoisonExplosion.cs:37` | `executionThreshold: 15` | **json (15)** | а-дрейф |
| Тёмный покров | `Stacks` | `RegisterDefault(3)` — `DarkShroud.cs:32` | `stacks: 4` | **json (4)** | а-дрейф |
| ВСЕ 8 способностей Int | шансы/капы стадий | `_baseStageChances {0.5/0.25/0.05}`, `_stageChanceCaps {1/0.65/0.4}` — `MulticastActivation.cs:19,22` | нет | код | **в** |
| Вся книга | пол перезарядки | `MinimumCooldown = 1f` — `AbilityParameter.cs:131` | нет | код | в (осознанно, задокументировано) |

## Аугменты: числа мимо записи (`AbilityProvider.Upgrades.cs`, `GetValueOrDefault`)

Живые код-числа (ключа в записи нет): `Augment_Poison_Attack_Series.poisonPotency = 0.7f` (`:137`, читает `PoisonOnHitRider.cs:47`); `Augment_Leach_On_Crit.duration = 3` + `maxStacks = 1` (`:269-270`). Дрейф-дефолты (json есть и побеждает): Reduce_Cost 0.3 (`:60`), Deep_Freeze_Spread 0.5 (`:346`), DS_Two_Attacks_Apply_Buff 0.25 (`:253`), Reduce_Execution_Threshold 0.25 (`:387`), Leach_On_Crit.amount 0.15 (`:271`). Остальные ~40 строк и вся `ParameterAugments.cs` — чисто. Behaviour-реестр — эталон (Unbuildable отказывает дублям канона, EffectNumbers отбрасывает чужие ключи).

## Чисто (числа целиком из json)

Возрастающее давление, Отравляющее покрытие, Двойной удар, Арес, Дикобраз, Удар головой, Жертвоприношение, Перегрузка, Ледяная эгида, Разряд, Статический доспех, Помощь близнеца (щит), Костяные волки. Нейтральные базы (Effectiveness 1, бонусы 0 и т.п.) — объявленные точки входа аугментов, в данные не переносятся.

## Ноль-дефолты `== 0f ? X : value` (законный авторский ноль молча переписывается)

`DamageOverTurnEffect.cs:36` → 0.7 (**единственный стреляющий сегодня**: Банка яда, Близнец); `PoisonCoatingEffect.cs:27` → 0.7 (не стреляет — Покрытие передаёт 0.45 из json); `CurseEffect.cs:19` → 150 (= канон); `SlownessSeal.cs:19` → 1 (= канон); `OnEdgeEffect.cs:21` → 0.03 (канонической строки нет).

## Вердикт по примерам владельца

1. **Крит-шанс Критического расчета — подтверждён: только в коде** (0.15, `CriticalCalculation.cs:19→36`). Усугубляет: сам `Effect_Crit_Calculation_Buff` отсутствует и в `EffectsData.json`, и в реестре `EffectProvider.cs:29-160` — мимо всей канон-машины (без потолка стеков, силы диспела, гарда пейринга).
2. **Duration 1 (код) vs 3 (json) — живого бага НЕТ**: `base` регистрирует json первым, `RegisterDefault(1)` — no-op на занятом ключе; в рантайме 3. Единица — мёртвая лгущая строка. Зато рядом настоящий (б): `additionalDurationAmount: 1` из json не читается никем — продление зашито `Extend(1)`.

## Топ-5 по значимости

1. **Тик яда/горения способностей идёт мимо канона**: Банка яда 0.7 вместо 0.35 (×2 к DoT), Близнец 0.7 вместо 0.45 — способность строит `new DamageOverTurnEffect(...)` напрямую, не через `IEffectProvider`; правка баланса в json на них не действует. ФИКС МЕНЯЕТ ОТГРУЖЕННЫЙ БАЛАНС — решение владельца.
2. **Крит-шанс КР + эффект целиком вне канона** (п. выше).
3. **Шансы/капы стадий стойки Int — код-онли** (множитель выхода восьми способностей).
4. **Дрейф дефолтов** (Осколки ×6, Армагеддон, Взрыв яда, Серия атак, Покров + 5 аугментных) — рантайм прав, код лжёт; мина при `RegisterDefault` до `base`.
5. **Скрытые числа стадий/аугментов без записей** (стан Глыбы, прыжок Цепной, ×2 за стан, Берсерк min/max, MissingHpStep, poisonPotency/duration двух аугментов, стаки Заморозки, порог обрыва серии).

## Непроверенное

Сборки/тесты (правило одного сборщика); Battle/Internal и стенды; NPC/лут/крафт/пассивки изнутри; Fireball/ManaDevour (мёртвые, не зарегистрированы); роллы копий аугментов (отдельный контур); показ значений (плейсхолдеры берут `Params.Keys` — числа вне набора параметров в описание физически не попадают); потолки стеков одного effect id из разных файлов (обе цифры из данных — соседняя тема).

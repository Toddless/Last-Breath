# Боевые слои — на что действует каждый

Справочник по коду, не по замыслу. Отвечает на один вопрос: **на что действует этот слой** — по какой причине урона, по какому типу, с каким капом, в какой точке пайплайна, кто читает.

Составлен 2026-07-30 сплошной сверкой с рабочим деревом (ветка `projects-clean-up`). Где докстринг расходится с телом метода — сказано отдельно, раздел 10.

> [!IMPORTANT] Как пользоваться
> Прежде чем писать механику, найди свой слой в разделах 2–7 и посмотри, **что он уже гейтит**. Дважды за один прогон механика уезжала в прод с гейтом, которого не было в дизайн-доках, и дважды это откатывалось. Если добавляешь новое правило — впиши сюда строку, иначе следующий агент будет догадываться.
>
> Боевой контур существует в **двух копиях**: `src/Battle/…` (эталон) и `src/Main/…`. Копии разошлись — см. 9.2.

---

## 1. Перечисления

### 1.1 `DamageCause` — `Core/Enums/DamageCause.cs:3-11`

| Член | Кто производит |
|---|---|
| `Attack` | `Calculations.ComposeAttackDamage:48` — **единственная** точка |
| `Ability` | 11 мест: `Armageddon:103`, `ChainLightning:118`, `Discharge:137`, `IceBlocks:163`, `IceShards:123`, `IpDamageRandomTargetStrategy:57`, `MulticastAbility:75`, `PoisonExplosion:66`, `SplashRandomTargetRider:32`, `TwinAssistAttack:47`, `StaticArmorEffect:102`; плюс `BaseNpc:724` (летальный добой) |
| `Effect` | `EffectsComponent:121` (тик DoT), `FuryEffect:47`, `IceAegisEffect:89`, `PorcupineBuffEffect:57` |
| `Passive` | `Player:389` / `BaseNpc:628` (лечение → `ConvertToDamage`), `BloodthirstyPassiveSkill:74`, `EchoPassiveSkill:98`, `IceMeteorPassiveSkill:41`, `MeteorPassiveSkill:49`, `PorcupinePassiveSkill:45` |
| `Item` | **ноль производителей** |
| `Environment` | **ноль производителей** |

**Причина ≠ происхождение.** Способность, бьющая через атаку, получает `Cause = Attack` и штамп `SourceAbilityId`. Гейт «только способности» обязан смотреть на оба признака — см. 2.4.

### 1.2 `DamageType` — `Core/Enums/DamageType.cs:6-18`
`[Flags]`, но используется как одиночный ключ словаря: `Sacred = 1`, `Blight = 2`, `Burning = 4`, `Poison = 8`, `Bleed = 16`, `Physical = 32`, `Fire = 64`, `Cold = 128`, `Lightning = 256`.

**Капкан снят:** у `Sacred` теперь свой бит (`1 << 0`), поэтому маски `(type & mask) != 0` работают для всех членов. Нулевого члена нет вовсе — `(DamageType)0` не тип, а забытое присваивание, и митигация кричит о нём через `Unruled`.

### 1.3 `AttackResults` — `Core/Enums/AttackResults.cs:3-8`
`Evaded = 0`, `Blocked = 1`, `Succeed = 2`.
**Дефолт — `Evaded`** (`AttackContext.Result:30`, автосвойство без инициализатора). До `ResolveAttackOutcome` контекст выглядит как «уклонились».

### 1.4 `Costs` — `Core/Enums/Costs.cs:5-11`
`[Flags] : byte` — `Mana = 1`, `Health = 2`, `Barrier = 4`. **Члена `None`/`0` нет.** Списание — `Player:272-287` (`switch` без `default`), проверка — `Ability:216-222` (`_ => false`). Нулевой `CostType` из данных = способность вечно недоступна, стоимость молча не списывается.

### 1.5 `StatusEffects` — `Core/Enums/StatusEffects.cs:5-65`
`[Flags] int`, одно поле на сущности (`Player:290-303`).

| Член | Боевой потребитель |
|---|---|
| `Stun` | да — `EnumConverterExtension:9,12`, `UnshackledPassiveSkill:29` |
| `Paralysis` | да — `Ability:316` |
| `Freeze` | пропуск хода да; **«take more damage» не реализовано нигде** |
| `Bleed` / `Poison` / `Burning` | да — `EnumConverterExtension:38-40` + фильтры ручек |
| `Vanished` | да — `EntitySpot:125` |
| `Blind` | **флаг не читается**; эффект бьёт декоратором на `Accuracy` (`BlindEffect:15-17`) |
| `Regeneration`, `Cursed`, `Fury` | только выставляются, **читателей нет** |
| `Rust`, `Confused`, `Charmed` | **ноль ссылок вообще** |

### 1.6 Две шкалы приоритетов

| | `ContextModifierPriority` | `Priority` |
|---|---|---|
| Значения | `Early −100`, `Normal 0`, `Late 100`, `Absolute 1000` | `Base 0`, `Weak 1`, `Strong 2`, `Absolute 3` |
| Что упорядочивает | мутаторы контекстов (7 пайплайнов) | декораторы параметров сущности и способностей |
| Кто сортирует | `ModifierHandlerComponent.ModifierList.Add:52` — `Sort` при каждом `Add` | `ModuleManager.AddDecorator:62` |

`Sort` — интросорт, **нестабилен**: при равных приоритетах порядок не определён. Для смеси «×%» и «+N» это меняет результат.
Докстринг `Priority.Absolute` обещает «only one of these can be within the chain» — **не проверяется нигде**, дедупликация идёт по `Id`.

---

## 2. Входящий урон

### 2.1 Порядок — `Battle/Internal/Player/Player.cs:354-381` (то же `BaseNpc:595-620`)

| # | Шаг | Может изменить |
|---|---|---|
| 0 | `_lastDamageSource = context.Source` | атрибуция убийства |
| 1 | `Calculations.ApplyDamageModifiers(context, this)` — штамп `Target`, затем мутаторы **защитника** | компоненты, `IgnoreResistances`, `IgnoreBarrier` |
| 2 | там же: мутаторы **источника**, **кроме самоурона** (`Source == Target` — тот же список, второй проход возводил бы каждую строку в квадрат) | то же |
| 3 | `CombatEvents.Publish(BeforeDamageTakenEvent)` | всё — подписчики держат ссылку на контекст |
| 4 | `Calculations.CalculateMitigation(context, this, CombatRolls)` — стрим защитника обязателен | значения компонентов |
| 5 | `_damageChain.Apply(...)` | поглощение щитом/барьером/стражем |
| 6 | `CurrentHealth -= remaining` | здоровье |
| 7 | `DamageTakenEvent`, затем при необходимости смерть | — |

**Защитник применяется РАНЬШЕ источника.** Множители атакующего умножают уже урезанный урон. Сортировка по приоритету идёт **внутри каждого списка независимо**: `Absolute` защитника отработает раньше `Early` атакующего.

**`IDamageContext.Target` (2026-08-24)** — получатель удара, штампуется ровно в `ApplyDamageModifiers`, единственной точке, через которую проходит любая дорога урона. Отсюда: «снижение полученного» гейтится по ПОЛУЧАТЕЛЮ (`Target == owner`), а не по «кто угодно, кроме источника», — прежнее чтение делало самоурон (сжигание Ярости, откат Эха, лечение→урон, летальный сигнал стража стадии) невидимым для защитных строк. Самоурон остаётся с ПРИЧИНОЙ своего механизма (`Effect`/`Passive`), поэтому «−X% урона от эффектов» на нём срабатывает; отдельной причины `Self` нет намеренно — она бы вывела самоурон из-под тех же строк. **Цена каста здоровьем — не урон** и в пайплайн не входит вовсе (`ConsumeResource(Costs.Health, …)`: Армагеддон, Жертва), иначе броня удешевляла бы цену.
**Исходящие мутаторы гейт не сменили** (`Source == owner`): на самоуроне владелец и источник — одно лицо, поэтому его исходящие строки применяются к собственному урону ОДИН раз (было два). Развилка «самоурон только получают, но не наносят» не закрыта.

Шаг 3 — точка, где сидит `EchoPassiveSkill`: он снимает свою долю **до** митигации.

### 2.2 Митигация по типам — `Calculations.MitigateComponent:124-138`

| Тип | Чем митигируется | Уважает `IgnoreResistances` |
|---|---|---|
| `Fire` / `Cold` / `Lightning` | своё сопротивление × (1 − пробитие источника) | **да** |
| `Burning` | **fire**-сопротивление | **нет** — ветка минует проверку флага |
| `Physical` | броня × (1 − `ArmorPenetration`) | н/п |
| `Bleed` | **броня**, вместе с Physical | н/п |
| `Poison` | **poison**-сопротивление × (1 − пробитие источника) | **нет** — ветка минует проверку флага |
| `Sacred` | ничем | н/п |
| `Blight` | ничем (и мимо щита/барьера — см. 2.3) | н/п |

Ветки Sacred/Blight/Poison — **явные**; тип без своей ветки проходит насквозь с записью в Tracker (`Unruled`), а не наследует «не режется ничем» молча. Пара «сопротивление яда + пробитие» намеренно лежит ОТДЕЛЬНО от `s_resistanceByType`: снимает эту таблицу именно флаг `IgnoreResistances`, а тик ДоТа — не атака.

Формулы: `resist = target[R] * (1 − source[P])`, затем `damage * (1 − resist)`. Броня: `damage * (1 − A/(A+10000))`, `ArmorScalingFactor = 10000f` (`Calculations:20`) — асимптотика, верхнего капа у параметра нет.

### 2.3 Цепочка поглощения — `Core/Battle/DamageResolution/DamageResolutionChain.cs:30-41`
Порядок: **щит → барьер → страж стадии**. Ранний выход при `Total <= 0`.

Цепочка берёт удар **из контекста** и сама режет его надвое (`AbsorptionSplit.Of`): `Absorbable` — то, что поглощают слои по дороге к здоровью, `Bypassing` — типы, бьющие в здоровье напрямую (сейчас только `Blight`). Скаляра в `Apply` нет — разложка и число не могут разойтись.

| Слой | Съедает | Обход |
|---|---|---|
| `ShieldAbsorptionLayer:12-23` | до `IShieldEffect.Strength` первого щита, из `Absorbable` | **флага обхода нет**; чистый `Bypassing`-удар щита не касается вовсе |
| `BarrierAbsorptionLayer:12-22` | `min(CurrentBarrier, Absorbable)` | уважает `IgnoreBarrier` (весь удар мимо ОДНОГО слоя) |
| `StageGuardLayer:18-28` | не поглощает, а **отменяет оверкилл** до `FloorHealth` | читает `Total` — пол держит и Скверну |

### 2.4 Подавление — `Calculations.ApplySuppression:80-120`

| Свойство | Значение |
|---|---|
| Гейт | **происхождение от способности**: `Cause == Ability` ИЛИ непустой `SourceAbilityId` |
| Что читает | `SuppressChance` и `Suppress` **цели** |
| Ролл | один на удар, до цикла по компонентам |
| Место | хвост `CalculateMitigation` — после митигации, **до** цепочки поглощения |
| Капы | `Suppress` 0..0.75, `SuppressChance` 0..1 |
| Ранний выход | `chance <= 0 \|\| suppression <= 0 \|\| TotalDamage <= 0` |

**Под подавлением:** прямые `Cause = Ability` (Армагеддон, Цепная молния, Разряд, Ледяные осколки, Глыба льда, Взрыв яда, мультикаст, ассист Близнецов, сплеш-райдер) и **штампованные атаки** (Двойной удар, Серия атак, Ярость берсерка, Удар в голову, Возрастающий натиск), плюс `StaticArmorEffect`.

**Вне подавления:** базовая атака; реакции `CounterAttackPassiveSkill`/`ChainAttackPassiveSkill` — `AttackContext.CreateReaction:63-68` **не копирует штамп**; весь `Cause = Effect` (тики DoT, отражения); `Cause = Passive` (откат Эха, конверсия лечения); `Item`; `Environment`.

**Известная проблема:** `BaseNpc.Kill()` (обе копии, `:724`) строит `Sacred = CurrentHealth` с `Cause = Ability` в расчёте на подрезку стражем стадии. Подавление стоит раньше цепочки, поэтому летальный сигнал может не сработать. Урон используется как управляющий сигнал — нужен путь принудительной смерти мимо митигации.

**Стрим RNG:** параметр обязателен, каждый боец передаёт **свой** генератор — тот же, на котором сидит его `GetDamage()`. Ролл идёт за защитника. Бой по сиду по-прежнему невоспроизводим, но по другой причине: стрим именной, а не сидовый — каждая сущность делает `Randomize()` при готовности. Полная проводка сида — отдельная задача.

---

## 3. Атака

### 3.1 Порядок
1. `BattleArena.CreateAttackContext:760` — **свой RNG на каждую атаку**.
2. Сид компонентов (`AttackContext:77-87`): `Physical = baseDamage`, плюс флэт `FireDamage`/`ColdDamage`/`LightningDamage` атакующего, если `> 0`.
3. `AttackContextScheduler.RunQueue:34-60` — предохранители `MaxReactionDepth = 25`, `MaxAttacksPerDrain = 256`.
4. `Attacker.Attack:346-352` — `BeforeAttackEvent`, затем **бросок крита**.
5. `Target.ReceiveAttack:315-344` — `ResolveAttackOutcome` → `CalculateInitialAttackDamage` → `ComposeAttackDamage` → `TakeDamage`.

Ability-scoped мутаторы атаки идут отдельным списком (`AttackModifierPipeline.ApplyAll:20-24`), **до** планирования.

### 3.2 Попадание — `Calculations.ResolveAttackOutcome`
Уклонение и блок — **два независимых броска** одного `Rnd`, оба через `ChanceRoll.Roll` (см. 3.5).
```
advantage = max(0, evade − accuracy)
шанс уклонения = advantage / (advantage + 10000)
```
`EvasionScalingFactor = 10000f`. **Точность ≥ уклонения → уклонения нет вовсе.** Блок — плоский шанс, точность не учитывает, кап 0.9.

Вердикт возвращается значением; отменяющих событий исхода нет (`TargetEvadedAttackEvent`/`TargetBlockedAttackEvent` снесены — подписчиков не было). «Нельзя уклониться/блокировать» — флаги контекста `IsUnevadable`/`IsUnblockable`.

### 3.3 Крит
`RawCriticalChance` — от `CriticalChance` (кап 0..1). `RawCriticalDamage` — от `CriticalDamage` (**без капа**). Применение — `CalculateInitialAttackDamage`: `ScaleDamage(1 + (RawCriticalDamage − 1) × (1 − CriticalDamageMitigation))`, множит **все** компоненты.

**Митигация режет надбавку, а не весь множитель:** при `CriticalDamageMitigation = 1` (кап) крит бьёт как обычный удар, а не в ноль; пол — обычный удар, инверсия («крит слабее обычного удара») невозможна.

**Множитель заменяет урон, а не добавляет:** база 1.5 даёт ×1.5; при `RawCriticalDamage = 0` крит **обнуляет** удар.

### 3.4 Мост «атака → урон» — `ComposeAttackDamage:44-53`
Единственная точка. Переносит компоненты как есть, ставит `Cause = Attack`, `IsCrit`, **`SourceAbilityId`**.

### 3.5 Броски шанса — `Core/ChanceRoll.cs`
Единая точка всех «получилось/не получилось» боевого свода (уклон, блок, подавление, крит, доп. атака, контратака, шансы пассивок/аугментов/стадий босса): правило `бросок <= шанс`, нулевой шанс не проходит никогда, бросок берётся всегда (гард «не жечь стрим» ставит вызывающий — подавление, `SpreadFreezeChance`, `IceBlocks.CooldownResetChance`, откат перезарядки `PorcupineBuffEffect`).
**Удача — адресное состояние сущности**: реестр `параметр → счётчик` (`IEntityParametersComponent.AddChanceLuck/RemoveChanceLuck/GetChanceLuck`), удачливый бросок = два броска и лучший, неудачливый = худший, источники разных знаков гасятся. Вне точки осознанно: лестница стадий мультикаста (кумулятивная семантика), не-булевы броски и всё вне боя (лут, крафт, торговля, нарратив, мировой AI).

---

## 4. Наложение эффекта — `Battle/Source/Effects/Effect.cs:66-100`

| # | Шаг | Мутабельно |
|---|---|---|
| 1 | `EffectApplicationContext(Caster, Target, this)` — только если `IsBonusStack == false` | — |
| 2 | **сторона кастера**: `Caster.ModifierHandler.Apply(application)` | сам эффект (`Duration`, `DamagePerTick`) + `BonusStacks` |
| 3 | бонус-стаки: `Copy().Apply(… IsBonusStack = true)` — клоны пайплайн **пропускают** | — |
| 4 | `IncomingEffectContext(Caster, Target, this)` | — |
| 5 | **сторона цели**: `Target.ModifierHandler.Apply(incoming)` | `Duration` + `Rejected` |
| 6 | **точка отказа**: `if (incoming.Rejected)` → `EffectResistedEvent`, выход | — |
| 7 | `Target.Effects.AddEffect(this)` — правила стакинга | — |
| 8 | `TryApplyStatusEffect(Status)` | битовая маска статусов цели |

Отказ ставит только `ControlResistanceModifier:26` — вешается на NPC (`BaseNpc:341`), **на игрока не вешается**.

**Две тонкости:** статус проставляется даже если эффект отклонён стакингом; `SubscribeUntilRemoved` молча выходит при `!IsApplied` — отклонённый стак ни на что не реагирует.

**Граница «наложение vs продление»:** продление СТОЯЩЕГО эффекта — только `Effect.Extend(turns)`, суммарно не больше `ExtensionBudget` инстанции (`CombatRules.json → effects.maxExtendedTurns`); метод возвращает фактически добавленное, 0 при исчерпанном бюджете и у отжившего эффекта (`Duration <= 0` — такой на носителе уже не стоит, конец хода, обнуливший счётчик, его и снял; продлевать там нечего). Мутаторы наложения (шаги 2 и 5 таблицы, `EffectDurationScale`/`EffectDurationBonus`, `ControlResistanceModifier`) и рефреш `max(existing, new)` при повторном наложении — по другую сторону границы: они формируют длительность, с которой эффект встаёт, и бюджета не платят. Бюджет — у инстанции: два стака = два бюджета, `Copy()` даёт новый инстанс со своим.

### 4.1 Стакинг — `Core/Entity/Components/EffectsComponent.cs`
- Без `Source` эффект **не добавляется никуда** (`:41`).
- `MaxStacks` считается **глобально по цели**, по всем источникам (`:46`).
- Многостаковые: при переполнении вытесняется глобально самый старый одноимённый (`:150`).
- Одностаковые (`:158-178`): новый сильнее → замена; иначе **рефреш длительности** `max(existing, new)`, новый инстанс выбрасывается. База `Effect.IsStronger` — `false`, то есть по умолчанию только рефреш.
- Тики DoT: группировка по статусу и кастеру, мёртвый кастер пропускается, суммарный урон одним контекстом `Cause = Effect`. Тип — `status.GetDamageType()`, всё кроме Bleed/Burning/Poison → `Physical` (безымянный ДоТ отвечает броне, а не тикает неснижаемым).
- `TurnEnd`: действие эффекта (переопределение) → `if (Duration > 0) Duration--;` → `if (Expired) Remove();`. **N ходов = N концов хода носителя**, на (N+1)-м эффекта уже нет; кламп держит счётчик на нуле, иначе эффект, положенный с `Duration 0`, ушёл бы в −1 и не истёк никогда.
- Отмена ожидающего тика при снятии (`RemoveEffect`) сужена до `Duration > 0`: развеянный стак теряет забронированный тик, истёкший — сохраняет (это тик его последнего хода).
- **Свежий чужой эффект пропускает конец хода, в который лёг.** Компонент знает обе кромки хода носителя (`TriggerTurnStart`/`TriggerTurnEnd`) и отдаёт факт как `IsOwnersTurn`; `Effect.Apply` при посадке ЧУЖОГО (`Caster != Target`) эффекта на бойца с открытым ходом зовёт `SitOutThisTurnEnd`. Пропускается ВЕСЬ конец хода — тик DoT и лечение вместе с декрементом, — ровно один и только ближайший. Реакция (слом Ледяной эгиды → заморозка ломающего) стоит ему СЛЕДУЮЩЕГО хода, а не хвоста уже сыгранного. Вне правила осознанно: селф-касты (иначе одноходовый селф-баф снова живёт лишний ход) и рефреш стоячего стака (тот живёт по часам носителя с момента посадки). Долг гасится тем же концом хода; `RemoveAllEffects` чистит только долг — ход носителя он не закрывает (этой же дорогой босс сбрасывает эффекты ПОСРЕДИ своего хода).

---

## 5. Активация способности

Мутабельны ровно три поля `IAbilityActivationContext`: `Cost`, `CostType`, `Cooldown`.

Порядок `Ability.Execute:142-163`: `CastId` → гейт паралича → `BuildActivationContext` → `ApplyActivationMutators` → `StartCooldown` → `ConsumeResource` → `AbilityActivatedEvent` → `ExecuteInternal` → райдеры.

`ApplyActivationMutators:308-312`: **сначала ability-scoped, потом entity-scoped**.
`StartCooldown:236` — `(int)cd`, усечение, не округление.

### 5.1 `IsPreview`
Превью не платит, не доставляет, **не несёт `Field`, `Targets`, `Rnd`**. Путь: `PreviewActivation:285-291` → `IsEnoughResource:212-223` → `CanActivate:225`.

Флаг уважают **ровно три** мутатора: `ChanceCooldownResetActivationContextModifier:15`, `ChanceFreeCastActivationContextModifier:15` (оба роллят), `NextAbilityCooldownEffect:49` (самопотребляется). Остальные детерминированы и игнорируют флаг осознанно.

**Следствие:** превью показывает **полную** цену, а каст может оказаться бесплатным — расхождение в безопасную сторону.

### 5.2 Изнеможение — `Core/Modifiers/Context/ExhaustionModifier.cs:16-32`
Приоритет `Normal`. Формула: `Cost *= 1 + Stacks * CostIncreasePerStack`.
Проводка (`Battle/Source/ExhaustionGrant.cs:16-28`): `AbilityActivatedEvent` → `Stacks++`; `TurnEndEvent` → спад на `DecayPerTurn`; `BattleEndEvent` → сброс.
Мультикаст даёт **один** стак. Верхнего капа стаков нет. При `CostIncreasePerStack <= 0` не вешается вообще.

---

## 6. Границы параметров — `EntityParametersComponent.cs:20-35`

Тринадцать записей: `BlockChance` 0..0.9 · `CriticalChance` 0..1 · `AdditionalHitChance` 0..0.75 · `ArmorPenetration` 0..1 · `Suppress` 0..0.75 · `CriticalDamageMitigation` 0..1 · три сопротивления 0..0.8 · `SuppressChance` 0..1 · три пробития 0..1.

Параметр **без записи флорится нулём** (`ApplyBounds:114-115`). Применяется в индексаторе, то есть **на всех каналах чтения**: именованные свойства, `GetValueForParameter`, `ParameterChanged`, превью.

**Без верхней границы — 23 члена.** Держатся сами:

| Параметр | Чем |
|---|---|
| `Armor`, `Evade` | асимптотическими кривыми |
| `MulticastChance` | капами стадий (`MulticastActivation:22` — stage3 ≤ 0.65, stage4 ≤ 0.4) |
| `Health`, `Mana`, `Barrier` | ёмкости, текущие значения клампятся в сеттерах |

**Не держится ничем:** `CriticalDamage` (множитель урона напрямую), `PhysicalDamage`, `SpellDamage`, три флэт-элементальных, `Accuracy`, `HealthRecovery`, `ManaRecovery`, три атрибута.

---

## 7. Контекстный слой — 27 ручек

Раскладка по пайплайнам (`ContextModifierBindings.cs:24-62`):

| Пайплайн | Ручек из данных |
|---|---|
| `IDamageContext` | **16** |
| `IEffectApplicationContext` | **6** |
| `IAttackContext` | 2 — **не применяются в `Battle`, см. 9.2** |
| `IAbilityActivationContext` | 2 |
| `IHealContext` | 1 |
| `IIncomingEffectContext` | **0** |
| `IManaRecoveryContext` | **0** |

Все значения читаются лениво; единственное исключение — `AttacksIgnoreResistances` (флаг без значения).

### 7.1 Гейты ручек урона

| Ручка | Фильтр |
|---|---|
| `PhysicalToFire/Cold/Lightning` | `Source == owner`; **причина не фильтруется** — ловит и способности, и эффекты, и пассивки |
| `AttacksIgnoreResistances`, `AddedFire/Cold/LightningDamage`, `AttackSacredConversion` | `Source == owner` + `Cause == Attack` |
| `DamageTakenReductionFrom{Attack,Ability,Effect,Passive}` | `Target == owner` + своя причина; значение читается **один раз на удар** |
| `DotDamageTakenReduction` и три пер-статусных | `Target == owner`, маска статуса; **причина не проверяется**; значение читается **на каждый компонент** |

Тем же гейтом получателя живут кодовые `DamageTypeTaken` (Обморожение/Слабость) и `BastionPassiveSkill`. `CritDamageTaken` и `DamageImmunityEffect` гейта не имеют вовсе — на самоуроне они применялись дважды, теперь один раз.

Приоритеты: конверсии — `Absolute`; добавленный элементальный — `Late`; шансовые активационные — `Late`; остальные — `Normal`.

### 7.2 Ручки без записи в данных
`BleedDuration`, `BurningStacks`, `BurningDamageTakenReduction`, `PoisonDamageTakenReduction`, `BleedDamageTakenReduction` — код есть, данных нет.

### 7.3 Модификаторы только из кода, без `ContextParameter`
`IDamageContext`: `BarrierBypass` (гейт `Attack or Ability`), `CritDamageTaken`, `DamageTypeTaken`, `HitDamageDealt` (**только `Ability`**), `SacredDamageBonus` (**исключает `Effect`**).
`IAttackContext`: `Accuracy`, `FirstAttackCrit`, `LastAttackAlwaysCrit` (Late), `LeechOnCrit`, `UnblockableAttack` (Absolute), `UnevadableAttack` (Absolute), `PoisonAttack`.
`IHealContext`: `HealReduction`, `HealthRecovery`.
`IAbilityActivationContext`: `Exhaustion`, `FlatCost` (Early), `CostScale`, `CostType` (Absolute), `CooldownIncrease`, `NoCooldown`.

---

## 8. Параметрический слой — `Calculations:223-251`

```
sumAdditions      = базовое значение
sumIncreases      = 1
sumMultiplicative = 1
  Flat:           sumAdditions      += сумма значений ведра
  Increase:       sumIncreases      += сумма значений ведра
  Multiplicative: sumMultiplicative += сумма значений ведра
итог = (sumAdditions * sumIncreases) * sumMultiplicative
```

**`Multiplicative` складывается АДДИТИВНО внутри своего ведра.** Два модификатора по 0.2 дают ×1.4, а не ×1.44. Имя типа вводит в заблуждение.

`Flag` в числовой математике не участвует. Выключенные `IConditionalModifier` пропускаются, но остаются в списке.

Порядок цепочки: формула → декораторы (сортировка по `Priority`) → `ApplyBounds`.

### 8.1 Агрегаты — `Core/Enums/AggregateParameters.cs:13-20`
`AllResistance` → три сопротивления · `AllAttribute` → три атрибута · `AllDefence` → `Evade` + `Armor` · `AllResistancePenetration` → три пробития.
Значением агрегат не читается никогда; модификаторы разворачиваются на каждого члена при резолве.

В данных: `AllAttribute` и `AllResistance` есть. **`AllDefence` — ноль вхождений где-либо. `AllResistancePenetration` — только в формате отображения.**

---

## 9. Мёртвое и полумёртвое

### 9.1 Статы
- **`MoveSpeed`** — в `src/Battle` не читается **нигде**. Единственное чтение — движение в мире.
- **`AdditionalHitChance`** — единственное боевое чтение `ChainAttackPassiveSkill:22`. Без этой пассивки параметр не делает ничего.
- **`MulticastChance`** — только `MulticastActivation:31`, то есть только стойка Интеллекта.
- **`FireDamage`/`ColdDamage`/`LightningDamage`** — только флэт-затравка атаки (`AttackContext:16-18`). Способности плоский элементальный урон **не подхватывают**; `Increase` на них не увеличит урон заклинания ни на единицу.
- **`Barrier`** — только кап. Регенерации в бою нет вообще: `TurnRecovery` трогает лишь `HealthRecovery` и `ManaRecovery`. Барьер восстанавливают адресные механики и мировой отдых.
- **`Strength`/`Dexterity`/`Intelligence`** — прямых боевых чтений почти нет; конвертируются в `Health`/`Evade`/`Mana`.

### 9.2 Крупное: `IAttackModifier` с `ModifierHandler` не применяется
`ModifierHandlerComponent` держит `_attackModifiers` и умеет их применять, но в `src/Battle` **`Apply(IAttackContext)` не вызывается ни разу**. В `src/Main` вызов есть, но **после** `TakeDamage` — когда `IsUnevadable`, `RawAccuracy`, `RawCriticalDamage` уже не нужны.

Мёртвое следствие: ручки `HealthOnHit` и `ManaOnHit` прописаны в данных предметов и **не дают эффекта**. Классы `Accuracy`, `FirstAttackCrit`, `LastAttackAlwaysCrit`, `LeechOnCrit`, `UnblockableAttack`, `UnevadableAttack` работают **только** через ability-scoped `AttackModifierPipeline`; с предметов и эффектов — не работают.

### 9.3 Прочее
`DamageCause.Item` и `Environment` — ноль производителей, ручек снижения под них тоже нет. Флага обхода щита нет — аналога `IgnoreBarrier` у него не существует; мимо щита проходит только тип (`Blight`).

---

## 10. Докстринг против кода

| Где | Обещано | Фактически |
|---|---|---|
| `Calculations:57-58` | «outgoing modifiers (source) → incoming (target)» | Порядок **обратный**: `Player:358` — сначала target, `:360` — source |
| `StatusEffects:18-19` | `Freeze` — «skip turn **and take more damage**» | «take more damage» не реализовано нигде |
| `StatusEffects:42,50,54,58` | `Rust` — снижает броню; `Fury`, `Confused`, `Charmed` — своё поведение | Ни один флаг не читается; `Rust`/`Confused`/`Charmed` вообще без ссылок |
| `StatusEffects:22-23` | `Blind` — снижает шанс попадания | Флаг не читается; эффект работает декоратором на `Accuracy` |
| `Priority:17-20` | `Absolute` — «only one within the chain» | Не проверяется; дедупликация по `Id` |
| `ContextModifierBindings:16-21` | «каждая числовая ручка получает ленивый ридер» | Верно, но ручки `IAttackContext` вешаются в список, который в `Battle` не применяется |
| `IncomingDamageReductionContextModifier:14-15` | «читается один раз на удар» | Верно здесь; в парном `DotDamageTakenReductionContextModifier:23` — **внутри цикла**, на каждый компонент |
| `IShieldEffect:6` | «потребляется `TakeDamage` между митигацией и барьером» | Щитом занимается `DamageResolutionChain`, `TakeDamage` про него не знает |
| `ModifierValueType:13-16` | `Multiplicative` — «percent values» | Внутри ведра складывается аддитивно; имя обещает произведение |
| `ContextParameter:37-38` | `PhysicalTo*` — «доля физического урона **владельца**» | Причина не фильтруется: ловит способности, эффекты и пассивки, а не только атаки |
| `ContextParameter:23-24` | ссылка на ручку, «покрывающую все три DoT сразу» | Такого члена enum не существует |
| `EntityParameter:41-43` | агрегаты «never read as a value» | Верно; но `AllDefence` и `AllResistancePenetration` ещё и никогда не **пишутся** |
| `Effect:135-137` | — | Синтаксически битый XML-комментарий: `<summary>` не закрыт |
| `Calculations:65` | «callers that own a stream pass it in» | **Ни один** боевой вызов не передаёт стрим |

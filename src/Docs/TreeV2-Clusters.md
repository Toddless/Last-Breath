# Дерево v2: инвентаризация клиньев, гэпы, кластеры, маршруты трёх билдов

**Дата:** 2026-08-26
**Состояние дерева:** HEAD `70673e35` — `version 1, budget 65, spread 1.75, nodes 503, edges 616`.
**Статус:** аналитическая записка. Ничего в игровых данных не менялось; `PassiveTree.json` только читался. `TreeV1-Wedges.md` не редактировался.

**Продолжение** `src/Docs/TreeV1-Wedges.md` (2026-08-25). Оттуда наследуются: бюджеты экипировки (мид/лейт по трём фокусам), правило «заточка множит только `Flat`, мифик ×1.84», «редкость = число строк 2/3/4», «все процентные строки экипировки локальны → дерево = единственный глобальный процент», целевые правила Ц1–Ц4.

**Что изменилось с TreeV1.** Владелец перенёс в дерево **семь кластеров клина Сила/Ловкость** (+40 нод, +33 ребра): пять кандидатов TreeV1 (`All Defence`, `Accuracy|Armor Penetration`, `Block Chance|Suppress`, `Attributes|Health|Movespeed`, `Health Recovery|Evade`) и два собственных из дизайн-дока (`DoT Multiplier`, `Physical|Bleed Damage`). Клинья Ловкость/Интеллект и Интеллект/Сила не изменились — кандидаты TreeV1 для них в файл не переносились и повторены здесь в §3 в исправленном виде.

**Интерпретация задания (помечаю явно).** В заказе «оригинальные лучи (Сила, Ловкость, уклонение)» третьим назван параметр, а не стойка. Читаю это как **луч Ловкости** (уклонение — его сигнатурный параметр) и разбираю три чистых луча: Сила, Ловкость, Интеллект. Если имелось в виду что-то иное — см. §5, В1.

**Источники, проверенные по файлам в этой записке:**

| Что | Путь |
| --- | --- |
| Дерево | `src\SharedData\PassiveTree\PassiveTree.json` |
| Классы нод и лимиты содержимого | `src\Core\PassiveTree\NodeKindRules.cs`, `PassiveNode.cs`, `PassiveTreeDocument.cs` |
| Контекстные ручки и их биндинги | `src\Core\Enums\ContextParameter.cs`, `src\Core\Modifiers\Context\ContextModifierBindings.cs` |
| Свёртка контекстных строк дерева | `src\Core\PassiveTree\Context\PassiveTreeContextSource.cs`, `ContextKnobTotals.cs` |
| Формула параметра, капы, базы | `src\Core\Calculations.cs`, `src\Core\Entity\Components\EntityParametersComponent.cs`, `src\Battle\Internal\Player\Player.cs` |
| Атрибуты → параметры | `src\Core\Entity\Attribute\{Strength,Dexterity,Intelligence}.cs` |
| Агрегаты | `src\Core\Enums\AggregateParameters.cs` |
| ДоТ-механика | `src\Battle\Source\Effects\DamageOverTurnEffect.cs`, `PoisonCoatingEffect.cs`, `src\Core\Modifiers\Context\DotDamageBonusContextModifier.cs`, `src\SharedData\Effects\EffectsData.json` |
| Доп. атаки | `src\Battle\Source\PassiveSkills\ChainAttackPassiveSkill.cs`, `src\Battle\Source\DexterityStance.cs` |
| Канал пассивок дерева | `src\Battle\Source\PassiveGrantService.cs`, `src\Battle\Source\PassiveSkillProvider.cs`, `src\SharedData\PassiveSkills\PassiveCatalog.json` |

---

## §0. Восемь фактов механики, на которых стоит вся арифметика ниже

Всё проверено по коду; без этих фактов цифры кластеров не читаются.

**Ф1. Формула параметра.** `Calculations.CalculateModifiers`: `значение = (база + ΣFlat) × (1 + ΣIncrease) × (1 + ΣMultiplicative)`. Каналы **суммируются внутри себя**, не перемножаются. Значит и «мультипликаторы» ДоТа складываются, а не множатся.

**Ф2. Процент не работает без плоской базы.** Если у параметра база 0 и на экипировке его нет, любой `Increase` в дереве множит ноль. Это касается `BarrierRecovery`, `ArmorPenetration`, `Suppress`, `SuppressChance`, `BlockChance` (у игрока база 0.05), `CriticalDamageMitigation`, всех сопротивлений и всех контекстных «шансов». **Кластер под такой параметр обязан нести `Flat`.** Возвожу это в правило **Ц6** и применяю в §3.

**Ф3. Контекстные строки дерева складываются в ОДИН модификатор.** `PassiveTreeContextSource` держит по одному `ContextModifierEntry` на ручку, а его значение — `ContextKnobTotals.AsRead` = **простая сумма** всех взятых строк этой ручки. Тип значения контекстной строки (`flat`/`increase`) на дереве — **только формулировка в тексте**, на число он не влияет (`BucketOf` выбирается по признаку «флаг/не флаг»). Строки экипировки — отдельные модификаторы, поэтому дерево и шмот по одной ручке **перемножаются**, а внутри дерева — складываются.

**Ф4. Атрибуты дают ровно по одному параметру, плоско.** `Strength` → `Health +10` за очко, `Dexterity` → `Evade +3`, `Intelligence` → `Mana +5`. (В TreeV1 §5 было допущение «5 единиц своего параметра» — **это допущение было неверным**, здесь цифры по коду.) Плоская выдача атрибута попадает под все `Increase` дерева.

**Ф5. Доп. атака: один потребитель, жёсткий кап, рекурсия.** `AdditionalHitChance` читает ровно один код — `ChainAttackPassiveSkill`, который **выдаёт стойка Ловкости** (`DexterityStance`). После каждой успешной атаки катится шанс, и порождённая атака катится снова. Кап `AdditionalHitChance` = **0.75** (`EntityParametersComponent.s_bounds`), база игрока 0.05. Матожидание доп. атак на один удар = `p/(1−p)`: при 0.64 это 1.8, при капе 0.75 — 3.0. Доп. атака бьёт `PhysicalDamage × 0.9…1.1` и проходит полный пайплайн атаки, то есть **порождает все on-hit эффекты, включая наложение ДоТов**.

**Ф6. Как считается тик ДоТа.** `DamageOverTurnEffect`: `тик = пул × percentFromDamage × (1 + <Status>DamageMultiplier каста)`, дальше по пайплайну `DotDamageBonusContextModifier` домножает на `(1 + ctx.<Status>Damage)`. Пул: **яд — весь урон удара**, **кровотечение — только физическая компонента**, **горение — только огненная**. Канон (`EffectsData.json`): яд `percentFromDamage 0.35, duration 4, maxStacks 999`, кровотечение `0.8 / 3 / 999`, горение `0.45 / 3 / 999`. Потолка стаков фактически нет — длительность прямо конвертируется в число одновременно тикающих стаков.

**Ф7. Нода умеет выдавать пассивный навык, и ни одна не выдаёт.** `PassiveNode.PassiveId` + `Properties` → `PassiveGrantService` строит навык через `PassiveSkillProvider` (40 именованных фабрик + открытое семейство `StatPassiveSkill`). В `PassiveTree.json` **ноль** нод с `passiveId`. Ограничение: нода с `passiveId` **не имеет права нести строки** (`PassiveNode.WhyChannelsCollide`), но освобождена от нижнего порога числа строк (`Validate`).

**Ф8. Лимиты содержимого ноды.** `NodeKindRules`: Small — 1–2 строки, Notable — 1–3, Keystone — 0–3 + текст правила, сокеты и AbilityUnlock — 0. Очко стоит любая нода, кроме `Start` (три семечка стоек + хаб бесплатны).

---

## §1. Инвентаризация по клиньям (факт на HEAD)

### 1.1. Общая раскладка

| Kind | Всего | Из них пустых |
| --- | --- | --- |
| Small | 375 | 0 |
| Notable | 62 | 0 |
| SocketTier2 | 21 | 21 (по 7 на стойку) |
| SocketTier3 | 21 | 21 (по 7 на стойку) |
| AbilityUnlock | 18 | 18 (по 6 на стойку) |
| Start | 4 | 4 (хаб + 3 семечка) |
| Keystone | 2 | 1 (`keystone_7` — только текст) |

| Сектор | Нод | Small | Notable | **Кластеров** | Свободно до 15 |
| --- | --- | --- | --- | --- | --- |
| Сила (чистая) | 114 | 80 | 12 | **12** (+1 кейстоун) | 3 |
| Ловкость (чистая) | 113 | 79 | 12 | **12** (+1 кейстоун) | 3 |
| Интеллект (чистый) | 88 | 59 | 8 | **8** | 7 |
| Сила/Ловкость | 89 | 74 | 15 | **15** | **0 — клин полон** |
| Ловкость/Интеллект | 53+1 | 46 | 7+1 | **8** | 7 |
| Интеллект/Сила | 44 | 37 | 7 | **7** | 8 |

> «+1» у Ловкость/Интеллект — `notable_added_lightning_damage_1`, записанный как `stance=Intelligence, hybridStance=Dexterity`, тогда как его собственные малые (`small_added_ligthning_damage_1..5`, **с опечаткой «ligthning» в id**) записаны `Dexterity|Intelligence`. Кластер целый и связный, ориентация нотабла — единственная в дереве обратная. Это тот же вопрос В5 из TreeV1, **не трогал**.

Ценовая грамматика, снятая с файла (не изменилась с TreeV1): чистый луч — малая нода `Increase` ~5% профиля либо атрибут +4 плоско; клин — 5% сигнатурного / 3% вторичного, атрибут +2; нотабл — 1–3 строки, `Increase` 0.25–0.45 либо плоские добавки.

### 1.2. Клин СИЛА + ЛОВКОСТЬ — 15 кластеров (ПОЛОН)

Тема: тело и скорость, огонь, крит, физ+кровь. Единственный клин, где живёт **весь огонь игры**.

| # | Кластер (нотабл) | Нотабл даёт | Малых | Малые дают |
| --- | --- | --- | --- | --- |
| 1 | `notable_1` Crit Damage Mitigation | CritDmgMitigation +0.15 | 3 | ×3 CritDmgMitigation +0.05 |
| 2 | `notable_2` Health\|Health Recovery | Health +18%, HealthRec +25%, `CTX.HealthOnHit +25` | 4 | ×3 Health +3%, ×1 Str/Dex +2 |
| 3 | `notable_accuracy_1` Accuracy | Accuracy +700 плоско, +35% | 6 | ×5 Accuracy +5% |
| 4 | `notable_accuracy_armor_penetration_1` ★ | Accuracy +25%, ArmorPen +0.12 | 4 | ×2 ArmorPen +0.08, ×2 Accuracy +8% |
| 5 | `notable_added_fire_damage_1` | Fire +25%, `CTX.AddedFireDamage +35%` | 6 | Fire/AddedFire +5% |
| 6 | `notable_all_defence_1` ★ | **AllDefence +18%**, CritDmgMitigation +0.10 | 4 | ×4 AllDefence +4% |
| 7 | `notable_attributes_health_movespeed_1` ★ | AllAttribute +10, Health +12%, MoveSpeed +15% | 5 | ×3 AllAttribute +4, ×2 Health +4% |
| 8 | `notable_block_chance_suppress_1` ★ | BlockChance +0.04 и +15%, Suppress +0.10 | 5 | ×3 BlockChance +0.01, ×2 Suppress +0.04 |
| 9 | `notable_critical_chance_damage_1` | CritChance +25%, CritDamage +0.35 | 5 | CritChance +0.015 / +5% |
| 10 | `notable_dot_multiplier_1` ★ | **AllDoTDamageMultiplier +0.15 плоско** | 4 | ×4 AllDoT +4% |
| 11 | `notable_fire_damage_1` | Fire +35%, FireRes +0.12 | 5 | Fire +5%, FireRes +0.03 |
| 12 | `notable_fire_penetration_3` | FireResPen +0.25, Fire +25% | 6 | FireResPen +0.05, Fire +5% |
| 13 | `notable_health_recovery_evade_1` ★ | HealthRec +15%, Evade +20% **при HP<30%** | 5 | ×3 HealthRec +5%, ×2 Evade +4% |
| 14 | `notable_physical_bleed_damage_1` ★ | **Phys +32%, BleedDamageMultiplier +0.15** | 6 | ×3 Phys +7%, ×3 BleedMult +6% |
| 15 | `notable_physical_convert_to_fire_1` | Fire +25%, `CTX.PhysicalToFire +25%` | 6 | Fire/PhysToFire +5% |

★ — семь кластеров, добавленных владельцем в HEAD.

**Суммарно по клину, если взять всё:** Fire +205%, FireResPen +0.50, `CTX.AddedFireDamage` +0.60, `CTX.PhysicalToFire` +0.50, Accuracy +101% и +700, CritChance +50%, CritDmgMitigation +0.40, ArmorPen +0.28, AllDefence +34%, AllDoT +0.15/+16%, BleedMult +0.15/+18%, Phys +53%, AllAttribute +22.

### 1.3. Луч СИЛА — 12 кластеров + кейстоун

| # | Кластер | Нотабл даёт | Малых |
| --- | --- | --- | --- |
| 1 | `notable_health_1` | Health +18%, HealthRec +24%, Str +11% | 6 |
| 2 | `notable_health_recovery_1` | Health +25%, HealthRec +35% и +150 | 8 |
| 3 | `notable_health_recovery_11` | HealthRec +30% и +75 | 6 |
| 4 | `notable_health_regen_low_life_1` | HealthRec +25%, **+180 при HP<30%**, `CTX.HealingEfficiency +30%` при HP<30% | 8 |
| 5 | `notable_armor_10` | Armor +25% | 4 |
| 6 | `notable_block_chance_armor_1` | Armor +42% и **+1200 плоско при стойке Силы**, BlockChance +35% | 7 |
| 7 | `notable_armor_penetration_1` | ArmorPen +0.25 | 5 |
| 8 | `notable_physical_damage_1` | Phys +35%, ArmorPen +0.07 | 6 |
| 9 | `notable_accuracy_2` | Accuracy +35%, Phys +25% | 5 |
| 10 | `notable_burning_damage_1` | `CTX.BurningDamage +35%` | 6 |
| 11 | `notable_reduce_damage_taken_from_attack_1` | Armor +35%, Health +15%, `CTX.DamageTakenReductionFromAttack +35%` | 7 |
| 12 | `notable_reduce_dot_taken_1` | `CTX.DotDamageTakenReduction +35%` | 6 |
| K | `keystone_6` Unlimited Power | **Phys `Increase` 0.01 за единицу Силы**, HealthRec ×0.75 | 3 |

**Суммарно:** Health +127%, HealthRec +214% и +425 плоско, Armor +168% и +1200, ArmorPen +0.47, Phys +106%, Accuracy +65%, Str +52 плоско и +11%, BlockChance +0.14/+35%.
**На луче Силы НЕТ НИ ОДНОЙ строки `FireDamage`** — только `CTX.BurningDamage`. Весь огонь стоит в клине Сила/Ловкость.

### 1.4. Луч ЛОВКОСТЬ — 12 кластеров + кейстоун

| # | Кластер | Нотабл даёт | Малых |
| --- | --- | --- | --- |
| 1 | `notable_evade_7` | Evade +35%, AddHit +25% и +0.03 | 5 |
| 2 | `notable_additional_hit_chance_evade_1` | Evade +40%, AddHit +40% | 7 |
| 3 | `notable_10` | AddHit +0.05 и +25% | 3 |
| 4 | `notable_critical_chance_additional_hit_chance_1` | AddHit +0.05, CritChance +0.05, CritDamage +0.30 | 8 |
| 5 | `notable_crit_chance_9` | CritChance +0.05 и +35% | 5 |
| 6 | `notable_critical_damage_1` | CritDamage +0.50 | 8 |
| 7 | `notable_accuracy_3` | Accuracy +40% и +500 | 6 |
| 8 | `notable_8` Suppression | SuppressChance +0.15, Suppress +0.05 | 7 |
| 9 | `notable_evade_suppress_1` | Evade +40%, Suppress +10%, SuppressChance +25% | 7 |
| 10 | `notable_health_2` | Phys +15%, Health +25% | 5 |
| 11 | `notable_poison_damage_1` | `CTX.PoisonDamage +25%` | 5 |
| 12 | `notable_poison_damage_taken_1` | `CTX.PoisonDamageTakenReduction +30%` | 5 |
| K | `keystone_7` Poisonous Bite | **строк нет, только описание** | 2 |

**Суммарно:** Evade +177%, AddHit +0.13 и +177%, CritChance +0.10/+80%, CritDamage +0.80/+42%, Accuracy +65% и +500, Suppress +0.33/+10%, SuppressChance +0.30/+45%, `CTX.PoisonDamage` +50%, Dex +60.
**На луче Ловкости нет ни одной строки кровотечения** — кровь живёт в клиньях Л/И и С/Л.

### 1.5. Луч ИНТЕЛЛЕКТ — 8 кластеров

| # | Кластер | Нотабл даёт | Малых |
| --- | --- | --- | --- |
| 1 | `notable_mana_1` | Mana +31%, Int +11% | 8 |
| 2 | `notable_mana_recovery_1` | ManaRec +45%, `CTX.ManaOnHit +75` | 8 |
| 3 | `notable_barrier_1` | Barrier +40%, SpellDmg +15% | 5 |
| 4 | `notable_barrier_multicast_1` | Barrier +35%, Multicast +0.05 и +35% | 6 |
| 5 | `notable_multicast_chance_1` | Multicast +0.10 и +40% | 10 |
| 6 | `notable_spell_damage_1` | SpellDmg +40% | 5 |
| 7 | `notable_spell_damage_multicast_1` | SpellDmg +45%, Multicast +0.05 и +35% | 6 |
| 8 | `notable_cold_lightning_damage_1` | Cold +25%, Lightning +25% | 7 |

**Суммарно:** SpellDmg +162%, Multicast +0.45/+177%, Barrier +125%, Mana +71%, ManaRec +87%, `CTX.ManaOnHit` +165, Int +60 и +47%, Cold +55%, Lightning +55%.
Самый бедный сектор: **8 кластеров при 12 у Силы и Ловкости**, и ни одного защитного кластера, кроме барьера.

### 1.6. Клин ЛОВКОСТЬ + ИНТЕЛЛЕКТ — 8 кластеров

`notable_bleed_damage_1` (`CTX.BleedDamage +42%`, `CTX.BleedDuration +1`), `notable_bleed_damage_taken_1` (−32% получаемого кровотечения), `notable_convert_to_lightning_1`, `notable_evade_barrier_1` (Barrier +40%, Evade +30%), `notable_lightning_damage_1`, `notable_lightning_penetration_2`, `notable_mana_health_1` (Mana +40%, Health +40%, композит), `notable_added_lightning_damage_1` (обратная ориентация).

**Суммарно:** Lightning +185%, LightningResPen +0.55, `CTX.PhysicalToLightning` +50%, `CTX.AddedLightningDamage` +55%, `CTX.BleedDamage` +57%, `CTX.BleedDuration` +1, Barrier +58%, Evade +48%, Mana +55%, Health +55%.

### 1.7. Клин ИНТЕЛЛЕКТ + СИЛА — 7 кластеров

`notable_added_cold_damage_1`, `notable_cold_damage_1`, `notable_cold_penetration_1`, `notable_convert_to_cold_1`, `notable_health_barrier_1` (Health +40%, Barrier +40%), `notable_mana_health_recovery_1` (ManaRec +40%, HealthRec +40%, композит), `notable_mana_mana_recovery_1` (Mana +25%, ManaRec +15%, `CTX.ManaOnHit +25`, композит).

**Суммарно:** Cold +205%, ColdResPen +0.40, `CTX.AddedColdDamage` +60%, `CTX.PhysicalToCold` +50%, Barrier +55%, Health/HealthRec +55%, Mana +31%, ManaRec +79%.
Самый маленький клин (44 ноды) и самый однотонный: пять из семи кластеров — про холод.

---

## §2. Гэп-анализ

### 2.1. БЛОКЕР: семь новых кластеров Сила/Ловкость — острова

Все 40 нод, добавленных в HEAD, **не имеют ни одного ребра к остальному дереву**. Каждый кластер связан только сам с собой (цепочка малых → нотабл). Проверено обходом графа от `Start`: 40 нод недостижимы, дистанция `INF`. Валидатор `PassiveTreeDocument.Validate` этого **не ловит** — он ругается только на ноду с нулевым числом рёбер, а у островов рёбра есть, просто ведут внутрь.

Следствия: `AllDefence`, `AllDoTDamageMultiplier`, `BleedDamageMultiplier`, `AllAttribute`, `MoveSpeed`, `BlockChance` и половина `ArmorPenetration` **в игре сейчас недоступны вообще**. Билд (в) без этого не собирается (§4.3).

Кандидаты на привязку — по фактической раскладке (медиана длины ребра в дереве **26**, p90 — **72**):

| Кластер-остров | Предлагаемое ребро | Длина |
| --- | --- | --- |
| All Defence | `small_all_defence_2` ↔ `small_physical_convert_to_fire_1` | 28 |
| Accuracy\|Armor Penetration | `small_accuracy_armor_penetration_3` ↔ `small_physical_convert_to_fire_2` | 29 |
| Physical\|Bleed Damage | `small_physical_damage_10` ↔ `small_fire_damage_5` | 17 |
| Physical\|Bleed Damage (второй вход) | `small_bleed_damage_6` ↔ `small_fire_penetration_3` | 35 |
| Health Recovery\|Evade | `small_health_recovery_evade_2` ↔ `notable_fire_penetration_3` | 50 |
| Attributes\|Health\|Movespeed | `small_attributes_health_movespeed_2` ↔ `small_dex_strength_12` | 51 |
| DoT Multiplier | `small_dot_multiplier_1` ↔ `small_fire_penetration_2` | 55 |
| Block Chance\|Suppress | `small_block_chance_suppress_3` ↔ `small_dex_strength_12` | 63 |

Все длины ниже p90; шесть из восьми — в районе медианы.

### 2.2. Параметры без единого носителя в дереве

`EntityParameter` (14 из 50 не встречаются ни на одной ноде):

| Параметр | Есть ли на экипировке | Комментарий |
| --- | --- | --- |
| **`BarrierRecovery`** | **нет** | Единственный параметр проекта с нулём носителей вообще. В коде реализован: плоское восстановление барьера в начале хода, кап — `Barrier`. Единственный источник в игре — пассивка `Passive_Skill_Soul_Devouring`. |
| `PoisonDamageMultiplier` | префикс амулета | В дереве только через агрегат `AllDoTDamageMultiplier` (клин С/Л, остров) |
| `BurningDamageMultiplier` | префикс амулета | то же |
| `PoisonResistance` / `PoisonResistancePenetration` | нет | Яд митигируется ядовитым сопротивлением (`Calculations.MitigateComponent`), пробить его нечем |
| `AllResistance` / `AllResistancePenetration` | суффиксы | агрегаты реализованы, в данных дерева не используются |
| `AllElementalDamage` | нет | агрегат реализован, ноль записей во всём проекте |
| `Damage` (самый широкий агрегат) | нет | ноль записей |
| `FireResistanceMaximum` и три сестры + `AllResistanceMaximum` | **нет** | Потолок сопротивлений — параметр (`ResistanceParameters.DefaultMaximum`, жёсткий кап 0.9), а не константа. **Поднять его сегодня нельзя ничем.** |

`ContextParameter` (10 из 27 ручек не встречаются):

| Ручка | Что делает | Естественное место |
| --- | --- | --- |
| `BurningStacks` | +N стаков горения за наложение (целое) | луч Силы (огонь — её фокус) |
| `EffectDurationScale` | множит длительность **всех** эффектов владельца | клин Л/И (ДоТы) |
| `CooldownResetChance` | шанс, что активация не оставит перезарядки | луч Интеллекта |
| `FreeCastChance` | шанс, что активация ничего не стоит | луч Интеллекта |
| `DamageTakenReductionFromAbility` | −урон от способностей | клин И/С |
| `DamageTakenReductionFromEffect` | −урон от эффектов | луч Силы |
| `DamageTakenReductionFromPassive` | −урон от пассивок | луч Силы |
| `BurningDamageTakenReduction` | −получаемое горение | луч Силы |
| `AttacksIgnoreResistances` | флаг: атаки игнорируют сопротивления | кейстоун, не кластер |
| `AttackSacredConversion` | доля урона атак → Sacred (не митигируется) | кейстоун, не кластер |

Из четырёх причин урона (`DamageCause`) дерево закрывает **одну** (`FromAttack`, луч Силы).

### 2.3. Канал `passiveId` не используется ни разу

Самый крупный неиспользованный рычаг. `PassiveGrantService` работает, `PassiveSkillProvider` знает 40 именованных фабрик, `PassiveCatalog.json` их перечисляет, `PassiveNode.PassiveId` парсится и сохраняется — и **ноль нод** в файле. Свежие коммиты владельца («the tree still awaits its souls») говорят, что он это знает.

Прямые следствия для трёх опорных билдов:

* **Кровотечение накладывается в игре только пассивкой** `Passive_Skill_Bleeding` (или предметным грантом). В дереве при этом стоят **три кластера, усиливающих кровотечение** (`notable_bleed_damage_1`, `notable_physical_bleed_damage_1`, малые `bleed_damage_*`). Это множитель без источника.
* **Единственная в коде связка «мана → урон»** — `Passive_Skill_Mana_Resonance` (касты наносят доп. урон, равный доле маны, потраченной за ход). Дерево её не выдаёт, значит «стак маны» сегодня — чистый ресурс каста.
* `Passive_Skill_Mana_To_Barrier`, `Passive_Skill_Poisoned_Claws`, `Passive_Skill_Soul_Devouring` (единственный источник `BarrierRecovery`) — та же история.

### 2.4. `keystone_7` «Poisonous Bite» — проза без механики

Нода несёт только `description`: «Each attack apply a Poison Stack (75% from total attack damage). Each stack lasts 3 turns.» **Строк нет, `passiveId` нет.** Взяв её, игрок не получает ничего. Между тем описание — дословный контракт `Passive_Skill_Poisoned_Claws` (поля `percentFromDamage`, `duration`). Билд (в) сегодня опирается на кейстоун, который не работает.

### 2.5. Перекос: стихии живут в клиньях, лучи пустые

| Элемент | На чистом луче | В клине |
| --- | --- | --- |
| Огонь (фокус Силы) | **0%** | Сила/Ловкость: +205% |
| Холод (фокус Интеллекта) | +55% (Инт) | Интеллект/Сила: +205% |
| Молния (фокус Интеллекта) | +55% (Инт) | Ловкость/Интеллект: +185% |

Это можно читать как намеренную грамматику («луч даёт вкус, клин — специализацию»), но у Силы вкуса нет вовсе: заявленный фокус «физ. урон **и огонь**» на её собственном луче представлен нулём строк огня.

### 2.6. Мелкие несоответствия

* **`AllDoTDamageMultiplier` — четыре малых в `Increase` перед нотаблом с `Flat`.** Цепь `small_dot_multiplier_1→2→3→4→notable`: малые множат плоский канал, который до нотабла даёт только амулет (0.05–0.15 базово). Первые четыре очка кластера почти ничего не стоят. По Ц6 стоит перевести два из четырёх в `Flat 0.03`.
* **Четыре кластера точности** на дерево: `notable_accuracy_1`, `notable_accuracy_2`, `notable_accuracy_3`, `notable_accuracy_armor_penetration_1`. Точность на экипировке уже самая насыщенная строка (лейт 1 334–2 484).
* **`notable_1` (CritDamageMitigation +0.15) дублируется** новым `notable_all_defence_1` (тот же параметр +0.10 плюс защита). Кандидат на слияние — это единственный способ освободить слот в полном клине.
* **Опечатка в id**: `small_added_ligthning_damage_1..5`.
* **Ловкость и `AdditionalHitChance`.** Луч один даёт +0.13 плоско и +177%. При базе 0.05 и мифик-шмоте +0.11 это `0.29 × 2.77 = 0.80` — **за капом 0.75**. Часть луча уже избыточна; добавлять доп. атаку ловкачу больше некуда.

---

## §3. Предложения по клиньям

### 3.0. Правила счёта

Наследую Ц1–Ц4 из TreeV1 (кластер клина = 25–40% лейт-бюджета экипировки по головному параметру; для параметров без экипировки — 75–85% от сопоставимого кластера чистого луча; сумма клина по параметру не обгоняет чистый луч; малая нода 3–7%, кластер 3–6 малых + нотабл) и добавляю два:

**Ц5.** Контекстные строки дерева складываются (Ф3) — не расписывать одну ручку в двух каналах ради «двух строк», это одна цифра.
**Ц6.** Параметр с нулевой плоской базой не масштабируется процентами (Ф2) — кластер под него обязан нести `Flat`.

Сводка предложений:

| Сектор | Стоит | Предлагаю | Станет | Запас до 15 |
| --- | --- | --- | --- | --- |
| Сила (чистая) | 12 | 2 (+1 опционально) | 14–15 | 1–0 |
| Ловкость (чистая) | 12 | 2 | 14 | 1 |
| Интеллект (чистый) | 8 | 4 | 12 | 3 |
| Сила/Ловкость | **15** | **0 — только правки** | 15 (14 после слияния) | 0–1 |
| Ловкость/Интеллект | 8 | 5 | 13 | 2 |
| Интеллект/Сила | 7 | 5 | 12 | 3 |

---

### 3.1. Клин СИЛА + ЛОВКОСТЬ — 15/15, только правки

**П-1 (блокирующая). Привязать семь островов.** Таблица рёбер — §2.1. Без этого шесть параметров и один агрегат недоступны в игре, а билд (в) не собирается.

**П-2. Слить `notable_1` в `notable_all_defence_1`** — они несут один параметр, и второй строго сильнее. Освобождает единственный слот в полном клине (три малых `notable_1` переходят цепью к соседу). Освободившийся слот предлагаю держать в запасе под будущий кейстоун клина.

**П-3. `notable_dot_multiplier_1`: два малых из четырёх перевести в `AllDoTDamageMultiplier Flat 0.03`** (Ц6). Итог кластера: `+0.06` плоско на подходе + `+0.15` на нотабле + `+8%` — цепь начинает работать с первого очка.

**П-4. Проверка Ц3 по `notable_physical_bleed_damage_1`.** Кластер даёт Phys +53% (нотабл 32% + три малых по 7%) против **+106%** на чистом луче Силы — 50%, в коридоре. Опасение TreeV1 (§4.4) снимается: тогда я считал по цифрам дизайн-дока, теперь по факту файла.

**П-5. Огонь.** Клин держит весь огонь игры, у луча Силы его нет (§2.5). Либо признать это грамматикой — тогда луч Силы должен получить огненный канал через горение (предложение S-3 ниже), либо перенести один огненный кластер на луч.

---

### 3.2. Луч СИЛА — предлагаю 2 обязательных + 1 опциональный

**S-1. «Плоть титана»** — служит билду (а)

* 4 малых: `Strength Flat 4` + `Health Increase 0.03`
* нотабл: `Health Flat 5 perParameter Strength` + `Health Increase 0.15` + `Strength Increase 0.10`

Итого **+16 Силы, +27% здоровья, +10% Силы**, и **+5 здоровья за каждую единицу Силы** сверх штатных 10 (Ф4).

*Обоснование.* Билд «стак Силы» сегодня не получает от дерева ничего за сам факт высокой Силы, кроме кейстоуна `Unlimited Power`, который конвертирует Силу в **урон**. Механизм `perParameter` реализован (`ScaledByParameterModifier`) и использован в дереве **один раз**. При Силе ~130 (база 5 + стойка 15 + дерево ~50 + шмот ~48, ×1.11) нотабл даёт +650 плоского здоровья, которые дальше множатся на весь `Health Increase` маршрута (≈×1.75–2.1) → **+1 140…1 370**. Против лейт-пула здоровья с экипировки (1 638 аффиксы) доля ~35% — в коридоре Ц1 для пятиочкового кластера. Это же делает Силу настоящим стат-стаком, а не просто множителем урона.

**S-2. «Несгибаемый»** — служит билду (а); закрывает две мёртвые ручки

* 3 малых: `CTX.DamageTakenReductionFromEffect Increase 0.05`
* 2 малых: `CTX.DamageTakenReductionFromPassive Increase 0.05`
* нотабл: `CTX.DamageTakenReductionFromEffect Increase 0.15` + `CTX.DamageTakenReductionFromPassive Increase 0.15` + `CriticalDamageMitigation Flat 0.08`

Итого **−30% урона от эффектов, −25% от пассивок, +8% смягчения крита**.

*Обоснование.* Из четырёх `DamageCause` дерево закрывает одну. `FromEffect` и `FromPassive` — две из десяти мёртвых ручек; ни на одном слоте экипировки их тоже нет, доля дерева 100%. По Ц2 против чистого `notable_reduce_damage_taken_from_attack_1` (+35%): 30% и 25% — 86% и 71%, в коридоре (кластер двухпараметрический).

**S-3 (опционально). «Погребальный костёр»** — фикс §2.5, закрывает ещё две ручки

* 3 малых: `BurningDamageMultiplier Flat 0.04`
* 2 малых: `CTX.BurningDamage Increase 0.05`
* нотабл: `CTX.BurningStacks Flat 1` + `BurningDamageMultiplier Flat 0.10` + `CTX.BurningDamage Increase 0.15`

Итого **+1 стак горения на каждое наложение, +22% мультипликатора горения, +25% урона горением**.

*Обоснование.* `BurningStacks` — мёртвая ручка (целочисленная, `views.WholeOf`); `BurningDamageMultiplier` не носит ни одна нода напрямую. Горение — заявленный фокус Силы, но на её луче нет ни строки огня (§2.5). Стаков у горения 999 (Ф6), поэтому «+1 стак за наложение» — прямое удвоение при одном наложении в ход. **Брать, только если владелец согласен, что огонь остаётся валютой клина**; иначе слот держим в запасе.

---

### 3.3. Луч ЛОВКОСТЬ — предлагаю 2, 1 слот в запас

**D-1. «Змеиный яд»** — служит билду (в)

* 4 малых: `PoisonDamageMultiplier Flat 0.04`
* нотабл: `PoisonDamageMultiplier Flat 0.12` + `CTX.PoisonDamage Increase 0.20` + `PoisonResistancePenetration Flat 0.15`

Итого **+28% мультипликатора яда, +20% урона ядом, +15% пробития ядового сопротивления**.

*Обоснование.* Три мёртвых параметра сразу. `PoisonDamageMultiplier` не носит ни одна нода (яд приходит только через агрегат `AllDoTDamageMultiplier` в чужом клине, и тот на острове). `PoisonResistancePenetration` — ноль носителей, при том что яд митигируется ядовитым сопротивлением, минуя флаг `IgnoreResistances` (по CLAUDE.md и `Calculations.MitigateComponent`), то есть пробить его сегодня нечем ничем. По Ц3 мультипликатор яда на чистом луче (+0.28) законно сильнее клиновой доли агрегата (+0.15). **Главный эффект: освобождает ДоТ-билд от 18-очкового крюка в клин Сила/Ловкость (§4.3).**

**D-2. «Кровопускание»** — служит билду (в); нода-пассивка

* 5 малых: `AdditionalHitChance Increase 0.04` + `CriticalChance Increase 0.04`
* нотабл: `passiveId: "Passive_Skill_Bleeding"`, `properties: { percentFromDamage: 0.25, duration: 3, maxStacks: 999 }`

*Обоснование.* Закрывает разрыв §2.3: в дереве три кластера усиливают кровотечение и **ни один его не накладывает**. `BleedingPassiveSkill` вешает стак на каждую успешную атаку — **включая доп. атаки цепочки** (Ф5), потому что обе подписаны на один и тот же `AfterAttackEvent`. Это и есть механическая склейка «ДоТ + много доп. атак», которую заказ требует сделать достижимой. `percentFromDamage 0.25` против канонических 0.8 — сознательно втрое ниже: пассивка стреляет с каждой атаки, а канон балансирует разовое наложение способностью.
*Ограничение:* нода с `passiveId` не имеет права нести строки (Ф7), поэтому весь стат кластера — на малых.

Запас: **1 слот.**

---

### 3.4. Луч ИНТЕЛЛЕКТ — предлагаю 4, 3 слота в запас

**I-1. «Родник барьера»** — служит билду (б)

* 4 малых: `BarrierRecovery Flat 40`
* нотабл: `BarrierRecovery Flat 240` + `BarrierRecovery Increase 0.30` + `Barrier Increase 0.15`

Итого **(160 + 240) × 1.30 = 520 барьера за ход, +15% барьера**.

*Обоснование.* `BarrierRecovery` — единственный параметр проекта с нулём носителей вообще (§2.2), при том что он заявлен в фокусе Интеллекта («восстановление маны/барьера») и реализован в коде. Процент здесь бесполезен без плоской базы (Ц6), поэтому кластер несёт `Flat` — и это первое место в игре, где барьер начинает восстанавливаться сам. 520/ход против лейт-пула барьера **3 989** (TreeV1 §3.3) = 13% пула за ход.
*Допущение Д-A:* считаю, что иначе барьер не регенерирует вовсе. Если в бою есть другой источник — цифру надо опустить (см. §5, В3).

**I-2. «Эхо чар»** — служит билду (б)

* 3 малых: `CTX.CooldownResetChance Flat 0.02`
* 2 малых: `CTX.FreeCastChance Flat 0.02`
* нотабл: `CTX.CooldownResetChance Flat 0.06` + `CTX.FreeCastChance Flat 0.05` + `MulticastChance Flat 0.03`

Итого **12% сброса перезарядки, 9% бесплатного каста, +3% мультикаста**.

*Обоснование.* Обе ручки реализованы (`ChanceCooldownResetActivationContextModifier`, `ChanceFreeCastActivationContextModifier`, обе катятся на `IAbilityActivationContext.Rnd`) и не встречаются нигде. Это «частота способностей» — сердцевина фокуса Интеллекта, и она не выражается ни одним `EntityParameter`. Цифры намеренно скромные: сброс перезарядки множит темп всего боя.

**I-3. «Стеклянная воля»**

* 4 малых: `AllResistance Flat 0.03`
* нотабл: `AllResistanceMaximum Flat 0.05` + `AllResistance Flat 0.08` + `Barrier Increase 0.12`

Итого **+20% ко всем четырём сопротивлениям, +5% к их потолку, +12% барьера**.

*Обоснование.* `AllResistance`, `AllResistanceMaximum` и четыре персональных максимума — ноль носителей. Потолок сопротивлений в коде **параметр**, а не константа (`ResistanceParameters`, жёсткий кап 0.9), и поднять его сегодня нельзя ничем; при этом на фулл-мифике сопротивления номинально уходят за +100% (TreeV1 §3.1, В2) и упираются в потолок, который некому двигать. Барьерный пул Интеллекта на 38–42% тоньше броневого и уклоненческого — платить за это правильно сопротивлениями, а не толщиной.

**I-4. «Чаша разума»** — служит билду (б); нода-пассивка

* 5 малых: `Mana Increase 0.05` + `ManaRecovery Increase 0.03`
* нотабл: `passiveId: "Passive_Skill_Mana_To_Barrier"`, `properties: { percent: 0.30 }`

*Обоснование.* Делает стак маны защитой: 30% каждого прихода маны конвертируется в барьер (`ManaToBarrierPassiveSkill`). Сегодня это только предметный грант. Для билда (б) это второй смысл `ManaRecovery` (+87% на луче) и единственная причина копить ману сверх стоимости кастов.

Запас: **3 слота.**

---

### 3.5. Клин ЛОВКОСТЬ + ИНТЕЛЛЕКТ — предлагаю 5, 2 слота в запас

Тема клина: повторы, реакция, кровь, тонкая защита. Молния, кровотечение-урон и Evade|Barrier заняты — обхожу.

**D/I-1. «Многоликий удар»** (перенос из TreeV1) — служит (б) и (в)

* 3 малых: `MulticastChance Increase 0.05`; 2 малых: `AdditionalHitChance Increase 0.05`
* нотабл: `MulticastChance Flat 0.04` + `MulticastChance Increase 0.18` + `AdditionalHitChance Flat 0.04`

Итого **+33% мультикаст, +0.04 плоско; +10% доп. атаки, +0.04 плоско**.

*Правка против TreeV1:* доп. атака на чистом луче Ловкости уже переливается за кап 0.75 (§2.6), поэтому здесь она — валюта для того, кто в луч Ловкости **не заходит** (интеллектуал с оружием), а не добавка ловкачу. По Ц2 против `notable_multicast_chance_1` (+0.10/+40%): 82%.

**D/I-2. «Затяжная рана»** (новый) — служит билду (в)

* 3 малых: `CTX.BleedDamage Increase 0.04`; 2 малых: `CTX.EffectDurationScale Increase 0.04`
* нотабл: `CTX.EffectDurationScale Increase 0.15` + `CTX.BleedDuration Flat 1` + `CTX.PoisonDamage Increase 0.15`

Итого **+23% длительности ВСЕХ эффектов, +1 ход кровотечения, +12% урона кровотечением, +15% урона ядом**.

*Обоснование.* `EffectDurationScale` — мёртвая ручка, множащая длительность каждого эффекта владельца. Для ДоТ-билда длительность — **второй множитель урона наравне с тиком**: стаков у ДоТов 999 (Ф6), поэтому лишний ход жизни стака = лишний одновременно тикающий стак. Яд по канону живёт 4 хода, кровь 3; +23% = +0.9 и +0.7 хода. Ручка не целочисленная (`views.Of`, не `WholeOf`), дробь не съедается флором — в отличие от `BleedDuration`, который флорится, поэтому он и написан целым.

**D/I-3. «Отравленный клинок»** (перенос из TreeV1)

* 3 малых: `CTX.PoisonDamage Increase 0.05`; 2 малых: `CTX.ManaOnHit Flat 20`
* нотабл: `CTX.PoisonDamage Increase 0.18` + `CTX.ManaOnHit Flat 50`

Итого **+33% урона ядом, +90 маны за удар**. Мостит ловкий яд к интеллектовой экономике маны. По Ц2 против `notable_poison_damage_1` (+25%) вместе с малыми луча (+50% суммарно): 66% — ниже коридора намеренно, мультипликатор яда остаётся привилегией луча (D-1).

**D/I-4. «Критическая формула»** (перенос из TreeV1, цифры опущены)

* 3 малых: `SpellDamage Increase 0.05`; 2 малых: `CriticalChance Increase 0.05`
* нотабл: `CriticalChance Flat 0.03` + `SpellDamage Increase 0.18`

Итого **+33% урона способностями, +10% увеличения крита, +0.03 крита плоско**. `SpellDamage` процентом на экипировке не существует. В TreeV1 нотабл был +0.20; опускаю до +0.18 — на чистом луче Интеллекта `SpellDamage` уже +162%, и Ц3 требует не обгонять.

**D/I-5. «Ускользающий разум»** (перенос из TreeV1, **исправлены каналы**)

* 3 малых: `Suppress Flat 0.03`; 2 малых: `Barrier Increase 0.05`
* нотабл: `Suppress Flat 0.06` + `SuppressChance Flat 0.05` + `Barrier Increase 0.20`

Итого **+0.15 подавления, +0.05 шанса подавления, +30% барьера** (кап `Suppress` — 0.75).

*Правка против TreeV1:* там подавление стояло в канале `Increase`, но база `Suppress` у игрока — 0 (`GetUnarmedBaseValue` его не называет), и процент множил бы ноль (Ц6). Перевожу в `Flat`.

Запас: **2 слота.**

---

### 3.6. Клин ИНТЕЛЛЕКТ + СИЛА — предлагаю 5, 3 слота в запас

Тема клина: стихия в тяжёлой руке. Холод и «здоровье+барьер» заняты — обхожу.

**I/S-1. «Рунная броня»** (перенос из TreeV1)

* 2 малых `Armor Increase 0.04`, 2 малых `Barrier Increase 0.04`
* нотабл: `Armor Increase 0.18` + `Barrier Increase 0.18`

Итого **+26% брони, +26% барьера**. Два толстых защитных пула, которых экипировка процентами не даёт вовсе. Доля 26% — нижняя граница Ц1, намеренно: кластер бьёт по двум пулам сразу.

**I/S-2. «Стихийный молот»** (перенос из TreeV1)

* 3 малых `AllElementalDamage Increase 0.04`; 2 малых `PhysicalDamage Increase 0.05`
* нотабл: `AllElementalDamage Increase 0.15` + `PhysicalDamage Increase 0.15`

Итого **+27% всего стихийного урона, +25% физического**. Единственный носитель `AllElementalDamage` во всём проекте. Ц3 соблюдён: +25% физа против +106% на луче Силы.

**I/S-3. «Тигель»** (перенос из TreeV1, **исправлены каналы**)

* 3 малых `CTX.HealingEfficiency Increase 0.05`; 2 малых `BarrierRecovery Flat 30`
* нотабл: `CTX.HealingEfficiency Increase 0.15` + `BarrierRecovery Flat 150` + `ManaRecovery Increase 0.20`

Итого **+30% эффективности лечения, +210 восстановления барьера за ход, +20% восстановления маны**. Второй из двух источников `BarrierRecovery` — сознательно: у параметра сейчас ноль носителей, одного кластера на всё дерево мало. Каналы исправлены по Ц6.

**I/S-4. «Незыблемость»** (перенос из TreeV1)

* 3 малых `CTX.DamageTakenReductionFromAbility Increase 0.05`; 2 малых `CriticalDamageMitigation Flat 0.03`
* нотабл: `CTX.DamageTakenReductionFromAbility Increase 0.15` + `CriticalDamageMitigation Flat 0.06`

Итого **−30% урона от способностей, +0.12 смягчения крита**. Третья из четырёх причин урона (после `FromAttack` на луче Силы и `FromEffect/FromPassive` в S-2).

**I/S-5. «Резонанс маны»** (новый) — служит билду (б); нода-пассивка

* 5 малых: `Mana Increase 0.04` + `ColdDamage Increase 0.03`
* нотабл: `passiveId: "Passive_Skill_Mana_Resonance"`, `properties: { rate: 0.12 }`

Итого **+20% маны, +15% холодного урона**, и **касты наносят доп. урон, равный 12% маны, потраченной за ход**.

*Обоснование.* **Единственная в коде связка «объём маны → урон»** (`ManaResonancePassiveSkill`: бонус ложится один раз на каст, по `CastId`, а пул потраченного растёт весь ход и включает самосжигание `Overload`). Без неё «стак маны» — только ресурс каста: ни одна нода дерева не превращает величину маны в силу. Вместе с абилками Интеллекта (`Overload` жжёт долю текущей маны, `Discharge` тратит всю) это и есть двигатель билда (б). Клин Интеллект/Сила — правильное место: холод уже здесь, а здоровье Силы платит за хрупкость мана-билда.
*Цифра `rate` — самая чувствительная во всём наборе:* при пуле маны ~11 700 (расчёт в §4.2) `Discharge` разово даёт +1 400 урона. См. §5, В4.

Запас: **3 слота.**

---

## §4. Маршруты трёх билдов

### 4.0. Метод счёта и бюджет

Бюджет из файла — **65 очков**. Очко стоит любая нода, кроме `Start` (`NodeKindRules.CostsPoint`), то есть **малые, нотаблы, кейстоуны, сокеты и ноды открытия способностей платятся одинаково**. Три семечка стоек и хаб бесплатны.

Маршруты посчитаны программно по фактическому графу: жадное дерево Штейнера от множества `Start` — цель за целью выбирается ближайшая, путь к ней покупается целиком, проходные малые считаются как купленные (они и есть). Это верхняя оценка оптимума и нижняя оценка реальной раскладки: игрок ещё берёт малые в сторону ради статов.

Измеренная стоимость сокетов: **1–3 очка за штуку** вместе с подходом; пять самых дешёвых на луче Интеллекта — 10 очков суммарно. Дальше считаю **8 очков за 4–5 сокетов**.

Все три маршрута ниже считаны **с гипотетической привязкой семи островов** по таблице §2.1. Без неё маршруты (а) и (в) не существуют в тех местах, где заходят в клин Сила/Ловкость.

---

### 4.1. Билд (а) — стак Силы и здоровья

**Маршрут (41 очко):**

| Шаг | Цена | Цель | Путь |
| --- | --- | --- | --- |
| 1 | 3 | `notable_health_1` | `small_strength_1 > small_health_2` |
| 2 | 3 | `notable_2` (клин С/Л) | `small_dex_strength_1 > small_health_9` |
| 3 | 4 | `notable_armor_10` | `small_strength_2 > small_strength_3 > small_armor_19` |
| 4 | 2 | `abilityunlock_11` Ares` Blessing | `small_strength_4` |
| 5 | 3 | `notable_health_recovery_11` | `small_strength_5 > small_health_recovery_19` |
| 6 | 2 | `abilityunlock_10` Head Butt | `small_strength_6` |
| 7 | 2 | `notable_health_recovery_1` | `small_health_recovery_7` |
| 8 | 3 | `notable_physical_damage_1` | `small_physical_damage_1 > small_physical_damage_5` |
| 9 | 4 | `notable_block_chance_armor_1` | `small_strength_8 > small_strength_9 > small_block_chance_armor_1` |
| 10 | 2 | `abilityunlock_12` Porcupine | `small_block_chance_armor_4` |
| 11 | 4 | `keystone_6` Unlimited Power | `small_strength_11 > small_strength_13 > small_strength_19` |
| 12 | 4 | `abilityunlock_13` Berserk Fury | через `notable_health_regen_low_life_1` |
| 13 | 5 | `notable_all_defence_1` ★ | `small_dex_strength_3 > small_dex_strength_4 > small_all_defence_3 > small_all_defence_4` |

**Состав:** 27 малых, 9 нотаблов, 1 кейстоун, 4 открытия способностей = **41 очко**.

**Что даёт дерево:**
`Health +75%` · `HealthRecovery +415 плоско, +194%, ×0.75 от кейстоуна` · `Armor +1200 плоско (в стойке Силы), +92%` · `AllDefence +26%` · `ArmorPenetration +0.07` · `BlockChance +0.06/+35%` · `CriticalDamageMitigation +0.10` · `CTX.HealingEfficiency +35%` · `CTX.HealthOnHit +25` · `PhysicalDamage +46%` · `Strength +50 плоско, +11%`

**Арифметика очков:** 41 (маршрут) + 8 (4–5 сокетов) = **49**. Остаток **16**.
На остаток вписываются оба моих кластера луча Силы: **S-1 «Плоть титана»** (5 нод + 1 подход = 6) и **S-2 «Несгибаемый»** (6 нод + 2 подхода = 8) = 14. Итого **63 из 65**.

**Что билд получает от S-1/S-2:** Сила в этой раскладке = `(5 база + 15 стойка + 50 дерево + 16 от S-1 + ~48 шмот) × 1.21` ≈ **162**. Тогда:
* `keystone_6` даёт **`PhysicalDamage Increase +1.62`** — то есть кейстоун один утраивает физический урон, и он же главный смысл стака Силы;
* атрибут даёт `162 × 10 = 1 620` плоского здоровья (Ф4), S-1 добавляет ещё `162 × 5 = 810`;
* всё это множится на `Health +75%` (+27% от S-1) → `(10 000 база + 2 430 от Силы + 1 638 шмот) × 2.02` ≈ **28 400 здоровья**.

**Вердикт: билд (а) достижим уже сегодня, с запасом ~16 очков; S-1/S-2 делают его тематическим, а не просто «крепким».**

**Дефициты экипировки (по пулам TreeV1):** процента здоровья, брони, восстановления и пробития брони нет **ни на одном слоте** — весь этот множитель приходит только из дерева. Плащ броневой базы не имеет вовсе (TreeV1 Д8) — для силового билда это мёртвый слот по базе.

---

### 4.2. Билд (б) — стак маны и холодного урона

**Маршрут (48 очков):**

| Шаг | Цена | Цель |
| --- | --- | --- |
| 1 | 3 | `notable_mana_mana_recovery_1` (И/С) |
| 2 | 3 | `notable_mana_health_recovery_1` (И/С) |
| 3 | 2 | `notable_health_barrier_1` (И/С) |
| 4 | 3 | `notable_cold_damage_1` (И/С) |
| 5 | 3 | `notable_cold_penetration_1` (И/С) |
| 6 | 4 | `notable_mana_1` (Инт) |
| 7 | 2 | `abilityunlock_Ice_Aegis_1` |
| 8 | 3 | `notable_barrier_1` (Инт) |
| 9 | 2 | `abilityunlock_Discharge_1` |
| 10 | 3 | `notable_spell_damage_1` (Инт) |
| 11 | 4 | `notable_mana_recovery_1` (Инт) |
| 12 | 2 | `abilityunlock_Overload_1` |
| 13 | 4 | `notable_added_cold_damage_1` (И/С) |
| 14 | 4 | `abilityunlock_Deep_Freeze_1` |
| 15 | 6 | `notable_convert_to_cold_1` (И/С) |

**Состав:** 33 малых, 11 нотаблов, 4 открытия = **48 очков**.

**Что даёт дерево:**
`ColdDamage +155%` · `ColdResistancePenetration +0.28` · `CTX.AddedColdDamage +45%` · `CTX.PhysicalToCold +50%` · `Mana +67%` · `ManaRecovery +113%` · `CTX.ManaOnHit +115` · `Barrier +88%` · `SpellDamage +68%` · `Health +43%`, `HealthRecovery +43%` · `Intelligence +46 плоско, +20%`

**Арифметика очков:** 48 + 8 (сокеты) = **56**. Остаток **9** — ровно на один мой кластер плюс подход.

**Прикидка величин.** Интеллект = `(5 + 15 стойка + 46 дерево + ~48 шмот) × 1.20 ≈ 137` → `+685` маны (Ф4).
Мана = `(5 000 база + 685 + 1 343 шмот) × 1.67` ≈ **11 740**.
`ManaRecovery` = `244 (шмот) × 2.13` ≈ **520 за ход**.
Холодный урон = `1 443 (шмот, лейт) × 2.55` ≈ **3 680**, плюс 50% конверсии физического в холод и 45% добавленного холода от атак.

**Дефицит и чем он закрывается:**

1. **Мана ничего не даёт, кроме кастов.** Ни одна нода не превращает объём маны в силу (§2.3). → **I/S-5 «Резонанс маны»** (6 нод + 2 подхода = 8 очков, ровно на остаток). Тогда `Discharge`, тратящий весь пул, разово добавляет `11 740 × 0.12 ≈ 1 400` урона, а `Overload` — долю от сожжённого.
2. **Барьер не восстанавливается.** `BarrierRecovery` — ноль носителей. → **I-1 «Родник барьера»** (+520 барьера в ход) и/или **I-4 «Чаша разума»** (30% каждого прихода маны → барьер, то есть `520 × 0.3 = 156` в ход поверх). Это второй смысл `ManaRecovery +113%`.
3. **Темпа нет.** `CooldownResetChance`/`FreeCastChance` — ноль носителей. → **I-2 «Эхо чар»**.

Всё это не влезает в один заход: **на остаток 9 очков берётся ровно один** из трёх (рекомендую I/S-5 — он единственный превращает заявленную тему билда в урон). Чтобы взять два, придётся снять `notable_convert_to_cold_1` (6 очков — самый дорогой шаг маршрута, и он же наименее нужен, если билд бьёт способностями, а не оружием).

**Вердикт: билд (б) достижим по холодному урону и по объёму маны, но «стак маны» сегодня не конвертируется в силу — тема билда закрывается только предложенным I/S-5.**

**Дефициты экипировки:** `SpellDamage` — только плоский (98/156), процента нет нигде; `Barrier`, `Mana`, `ManaRecovery` — только плоские; `BarrierRecovery` не роллится вообще. Барьерный пул на 38–42% тоньше броневого/уклоненческого (TreeV1 §3.3).

---

### 4.3. Билд (в) — ДоТ (яд/кровотечение) с большим числом доп. атак

**Механическая основа (Ф5, Ф6).** Стойка Ловкости выдаёт `ChainAttackPassiveSkill`; каждая успешная атака катит `AdditionalHitChance` и порождает новую атаку, которая катит снова. Каждая атака в цепочке — полноценная, поэтому накладывает всё, что вешается «на удар»: `PoisonCoating` (по стаку яда на атаку), `Passive_Skill_Bleeding` (по стаку крови на атаку). Стаков у ДоТов — 999. Отсюда цепочка множителей билда: **число атак × шанс наложения × тик стака × длительность стака**.

#### Вариант «как есть» — 55 очков и он не работает

Полный маршрут с двумя ДоТ-кластерами клина Сила/Ловкость:

| Шаг | Цена | Цель |
| --- | --- | --- |
| 1 | 3 | `notable_evade_7` |
| 2 | 4 | `notable_additional_hit_chance_evade_1` |
| 3 | 7 | `notable_poison_damage_1` |
| 4 | 3 | `abilityunlock_Poison_Coating_1` |
| 5 | 3 | `abilityunlock_Poison_Explosion_1` |
| 6 | 2 | `notable_critical_chance_additional_hit_chance_1` |
| 7 | 4 | `notable_bleed_damage_1` (Л/И) |
| 8 | 4 | `abilityunlock_Jar_of_Poison_1` |
| 9 | 5 | `notable_10` |
| 10 | 2 | `keystone_7` Poisonous Bite |
| 11 | **10** | `notable_physical_bleed_damage_1` (клин С/Л) |
| 12 | **8** | `notable_dot_multiplier_1` (клин С/Л) |

**55 очков** + 8 сокетов = **63 из 65**. И при этом:

* **шаги 11–12 сегодня физически невозможны** — обе цели на островах (§2.1);
* **шаг 10 не даёт ничего** — `keystone_7` несёт только текст (§2.4);
* **18 из 55 очков** (33% бюджета) уходят на крюк в чужой клин ради двух ДоТ-множителей.

**Вердикт по варианту «как есть»: билд (в) НЕ достижим. Не из-за бюджета, а из-за того, что его ключевые ноды не подключены к графу, а его кейстоун — проза.**

#### Вариант с предложенными кластерами — 64 очка и он работает

Маршрут без крюка в клин Сила/Ловкость — **37 очков**: `notable_evade_7` (3), `notable_additional_hit_chance_evade_1` (4), `notable_poison_damage_1` (7), `Poison Coating` (3), `Poison Explosion` (3), `notable_critical_chance_additional_hit_chance_1` (2), `notable_bleed_damage_1` (4), `Jar of Poison` (4), `notable_10` (5), `keystone_7` (2). Состав: 27 малых, 6 нотаблов, 1 кейстоун, 3 открытия.

Что даёт: `AdditionalHitChance +0.13/+120%` · `CTX.PoisonDamage +40%` · `CTX.BleedDamage +48%`, `CTX.BleedDuration +1` · `Evade +84%` · `CritChance +0.05/+6%`, `CritDamage +0.30` · `Dex +48`.

Плюс мои кластеры:

| Кластер | Нод + подход | Что добавляет |
| --- | --- | --- |
| **D-1 «Змеиный яд»** (луч Ловкости) | 5 + 1 = 6 | `PoisonDamageMultiplier +0.28`, `CTX.PoisonDamage +20%`, `PoisonResistancePenetration +0.15` |
| **D-2 «Кровопускание»** (луч Ловкости) | 6 + 1 = 7 | пассивка `Bleeding` (стак крови с каждой атаки), `AddHit +20%`, `CritChance +20%` |
| **D/I-2 «Затяжная рана»** (клин Л/И) | 6 + 2 = 8 | `EffectDurationScale +23%`, `BleedDuration +1`, `CTX.BleedDamage +12%`, `CTX.PoisonDamage +15%` |

**Арифметика: 37 + 6 + 7 + 8 = 58, плюс 6 очков на 3–4 сокета = 64 из 65.**

**Проверка величин.**

* **Доп. атаки.** `AdditionalHitChance = (0.05 база + 0.13 дерево + 0.11 шмот-лейт) × (1 + 1.20 + 0.20) = 0.29 × 2.40 = 0.696`. Матожидание доп. атак на удар = `0.696 / 0.304 = 2.3`, то есть **3.3 атаки за один замах**. Кап 0.75 не пробит; если добить его малыми луча (луч в сумме даёт +177%), выйдет ровно кап и **4 атаки за замах**.
* **Яд.** `PoisonDamageMultiplier = 0.28 (D-1) + 0.184 (префикс амулета, мифик) = 0.464`. `CTX.PoisonDamage = 0.40 (маршрут) + 0.20 (D-1) + 0.15 (D/I-2) = 0.75`. Тик стака = `урон удара × 0.35 (канон) × 1.464 × 1.75 = урон × 0.90`. При 3.3 атаках в ход и длительности 4 хода (×1.23 от `EffectDurationScale`) в установившемся режиме тикает **~14 стаков**, суммарно **~12.6 урона удара за ход** только ядом.
* **Кровотечение.** `Passive_Skill_Bleeding` с `percentFromDamage 0.25`, `BleedDamageMultiplier = 0.184` (только шмот; +0.15/+18% дерева остались в клине С/Л), `CTX.BleedDamage = 0.48 + 0.12 = 0.60`. Тик = `физ. компонента × 0.25 × 1.184 × 1.60 = физ × 0.474`, длительность 3 (+1 от `D/I-2`) ×1.23 ≈ 5 ходов. При 3.3 атаках — **~16 стаков в установившемся режиме**.
* **Пробитие.** `PoisonResistancePenetration +0.15` из D-1 — единственный в игре способ пробить ядовитое сопротивление.

**Вердикт: билд (в) достижим только после трёх правок — привязать острова клина Сила/Ловкость, дать `keystone_7` механику (`passiveId: Passive_Skill_Poisoned_Claws`), и добавить D-1/D-2/D/I-2. С ними он влезает в 64 очка из 65 и не требует крюка в чужой клин.**

**Дефициты экипировки:** ДоТ на шмоте — **одна строка на одном слоте**: префикс амулета `PoisonDamageMultiplier` / `BleedDamageMultiplier` / `AllDoTDamageMultiplier`, по конвенции слэша **один на выбор**, потолок +18.4% на мифике (TreeV1 §3.2). `AdditionalHitChance` на шмоте только плоский (+0.11 на лейте). Всё остальное для ДоТ-билда обязано прийти из дерева, и сегодня дерево эту часть держит на островах.

---

### 4.4. Сводка по бюджету

| Билд | Маршрут по факту | Сокеты | Мои кластеры | Итого из 65 | Достижим? |
| --- | --- | --- | --- | --- | --- |
| (а) Сила/здоровье | 41 | 8 | 14 (S-1, S-2) | 63 | **Да, уже сегодня** |
| (б) Мана/холод | 48 | 8 | 8 (I/S-5) | 64 | Да по числам, **нет по теме** без I/S-5 |
| (в) ДоТ + доп. атаки | 37 | 6 | 21 (D-1, D-2, D/I-2) | 64 | **Нет** без привязки островов и починки `keystone_7` |

Заметно, что все три упираются в 63–64 из 65: бюджет **точно откалиброван** под «луч + один заход в клин + 4–5 сокетов + 3–4 способности». Места на второй заход в клин нет ни у одного билда — это и есть причина, по которой 18-очковый крюк билда (в) его ломал.

---

## §5. Вопросы владельцу

**В1. «Оригинальные лучи: Сила, Ловкость, уклонение».** Прочитал третьим лучом **Ловкость** (уклонение — её сигнатурный параметр) и разобрал Сила / Ловкость / Интеллект. Если имелся в виду другой разрез — скажите, §1.3–1.5 пересобирается.

**В2. Острова — задумка или недоделка?** Семь новых кластеров клина Сила/Ловкость не связаны с деревом ни одним ребром. Считаю недоделкой (разметка перенесена, рёбра ещё нет) и предложил привязки в §2.1. Если это заготовка под другую топологию — скажите, привязки не нужны. **Файл не трогал.**

**В3. `BarrierRecovery`: сколько барьера в ход считается нормой?** Параметр реализован (плоское восстановление в начале хода, кап — `Barrier`) и **не имеет ни одного носителя в проекте**. Я взял 520/ход = 13% лейт-пула барьера (I-1) и ещё 210 в I/S-3, исходя из допущения Д-A: иначе барьер не регенерирует вовсе. Если он восстанавливается ещё как-то — цифры надо резать.

**В4. `rate` у «Резонанса маны» (I/S-5).** Это самая чувствительная цифра набора: при пуле ~11 700 маны `Discharge` разово даёт `rate × 11 700`. Взял 0.12 → +1 400. Оно должно быть сопоставимо с одним кастом или заметно больше?

**В5. Кейстоун `keystone_7` «Poisonous Bite» — прописать механикой?** Его описание дословно совпадает с контрактом `Passive_Skill_Poisoned_Claws` (`percentFromDamage`, `duration`). Предлагаю `passiveId: "Passive_Skill_Poisoned_Claws", properties: { percentFromDamage: 0.75, duration: 3 }` — тогда текст и механика сойдутся. Сейчас кейстоун не делает ничего.

**В6. Канал `passiveId` — открываем?** Четыре моих предложения (D-2, I-4, I/S-5 и починка `keystone_7`) стоят на нём. Сервис готов, каталог из 40 навыков готов, в дереве ноль таких нод. Если владелец хочет держать пассивки только за предметами — эти четыре предложения снимаются и часть дыр (кровотечение без источника, мана без конверсии в урон) остаётся открытой.

**В7. Огонь на луче Силы.** Весь огонь игры (+205%) стоит в клине Сила/Ловкость, на чистом луче Силы **ноль строк огня**. Это грамматика («луч даёт вкус, клин — специализацию») или перекос? От ответа зависит, брать ли S-3 «Погребальный костёр».

**В8. Кап `AdditionalHitChance` = 0.75.** Один чистый луч Ловкости уже переливает за кап (0.29 × 2.77 = 0.80). Кап поднимать, значения луча резать, или считать перелив нормальной платой за специализацию?

**В9. Потолок сопротивлений.** `*ResistanceMaximum` — параметры, а не константы, жёсткий кап 0.9, дефолт из `ResistanceParameters`. Поднять их сегодня нельзя ничем: ни ролла, ни ноды. Кластер I-3 «Стеклянная воля» — единственное предложение, которое их трогает. Это вообще задумано как игровой рычаг или как служебный параметр?

**В10. Считаются ли кейстоуны в лимит 15 на клин?** Считал, что нет (кластер = 3–6 малых + нотабл). При таком счёте лучи Силы и Ловкости имеют по 12 кластеров и по кейстоуну сверх лимита, а клин Сила/Ловкость **уже полон**: 15/15, любое новое содержимое там — только через слияние (П-2).

**В11. Опечатка в id:** `small_added_ligthning_damage_1..5` (клин Ловкость/Интеллект) — «ligthning» вместо «lightning». Косметика, но id — адрес ноды в сейве и в аллокации. **Не трогал.**

**В12. Валидатор не видит островов.** `PassiveTreeDocument.Validate` ругается только на ноду с нулём рёбер; связная подсеть, оторванная от `Start`, проходит молча. Стоит ли добавить проверку достижимости от `Start`? (Это правка кода, не дерева — вне рамок этой записки.)

**Переоткрытые вопросы TreeV1, всё ещё без ответа:** В2 (сопротивления масштабируются заточкой и уходят за +100%), В4 (кейстоунов в доке 16 при бюджете 9; в файле 2), В5 (ориентация `notable_added_lightning_damage_1`), В6 («эффективность бафов/дебафов» не существует в коде — ближайшее `EffectDurationScale`, теперь оно предложено в D/I-2), В9 (барьер тоньше брони и уклонения на 38–42%), В12 (`PassiveTreeDocument.DefaultBudget = 62` против 65 в файле).

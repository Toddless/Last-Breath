# Боевой урон и события

Статус: описание существующей реализации. Основа и границы статической сверки — [в отчёте](../Guidelines/SystemDocumentationAudit.md).

## Устройство и поведение

- **Урон атаки — словарь компонентов (2026-07-26)**: `AttackContext.DamageComponents` (тип → значение). При создании сеются Physical = базовый урон и флэт `Fire/Cold/LightningDamage` атакующего (префиксы оружия); бонус способности = `AddDamage(Physical, x)`, «+X% этой атаке» = `ScaleDamage` (Право первого, Primal Fury — умножают ВСЁ, включая стихию). Крит умножает все компоненты (`CalculateInitialAttackDamage`); в `DamageContext` компоненты переезжают через `Calculations.ComposeAttackDamage` — единственная точка для всех 4 копий ReceiveAttack. `AdditionalDamage` удалён. `FinalDamage` = фактически нанесённый урон ПОСЛЕ хита (лич/сплеш/DoT-та читают его).
- **Митигация** (`Calculations.MitigateComponent` — единственное место правил по типам): Fire/Cold/Lightning → резист × (1 − пенетрация источника: параметр `*ResistancePenetration` (агрегат `AllResistancePenetration`) + по-хитовый канал `IDamageContext.AddResistancePenetration`, сумма клампится 0..1 одной точкой `Calculations.Penetration`; пары «стихия→резист→пенетрация» — `Calculations.ElementalChannels`); Physical и **Bleed** → броня с `ArmorPenetration`; **Burning** → огненный резист, **Poison** → ядовитый резист, оба с пенетрацией и оба МИМО флага `IgnoreResistances` (это атакующая метка, а тик ДоТа — не атака: пара яда лежит вне `s_resistanceByType` именно поэтому); **Sacred** и **Blight** не митигируются. Агрегаты `AllResistance`/`AllResistancePenetration` включают яд. Пер-статусные ручки предметов: `Burning/Poison/BleedDamageTakenReduction` (маска в общем `DotDamageTakenReductionContextModifier`).
- CombatEventBus внутрення шина сущности. Необходима для работы внутренних систем сущности. Создается и умирает вместе с сущностью
- BattleEventBus внешняя боевая шина. Служит для комуникации между системами в рамках боя. Создается в начале боя, удаляется в конце.

Перед пенетрацией сопротивление ограничивается максимумом соответствующего сопротивления через `ResistanceParameters.Effective`. Броня уменьшает урон множителем `1 − effectiveArmor / (effectiveArmor + ArmorScalingFactor)`.

## Проверенные точки реализации

Пути относительно `src` LastBreath: `Core/Calculations.cs`, `Core/Enums/ContextModifierPriority.cs`.

## Связанные документы

- [Воспроизведение боя, анимации и VFX](../Combat/Presentation.md)
- [CombatExpansionArchitecture](../Combat/CombatExpansionArchitecture.md)
- [Параметры сущностей и модификаторы](../Parameters/CurrentSystem.md)
- [Активные способности](../Abilities/CurrentSystem.md)
- [Существующая система эффектов](../Effects/CurrentSystem.md)
- [Регистрация сервисов и межсистемные сообщения](../Services/CurrentSystem.md)
- [Карта документации](../README.md)

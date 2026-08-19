# Разбор реестра апгрейдов — таблица шага 9

> **АРХИВ: описывает систему улучшений до волны аугментов (2026-08) — не источник.**

Классификация всех 189 записей словаря `_abilityUpgrades` (`Battle/Source/Abilities/AbilityProvider.Upgrades.cs`). Каждая запись классифицирована по классу, который она создаёт, и перепроверена независимым проверяющим.

| Корзина | Записей | Доля | Что значит |
|---|---|---|---|
| Числовая | 104 | 55% | Декоратор по строковому ключу `AbilityParameterSet`; уезжает в json, кода не требует |
| Обобщаемая | 27 | 14% | Поведение над общим контрактом (райдер, стратегия, эффект); код пишется один раз на семейство |
| Именная | 58 | 31% | Обращается к членам конкретного класса способности; остаётся с явным `abilityId` |

Скрытых привязок (F-35): **30** — записи, которые выглядят общими, но на чужой способности инертны, падают или срабатывают не по адресу.

> Колонка «Привязка» называет класс, от которого запись зависит. У части записей это **семейный базовый класс** (`DamagingAbility`, `MulticastAbility`), а не конкретная способность: такая запись работает на всём семействе, и вопрос «чья она» решается не этой колонкой, а тем, под какой способностью она объявлена в `BaseAbilityData.json`.

---

## Числовая — 104

| Id | Стр. | Создаёт | Привязка | Почему | Скрытая |
|---|---|---|---|---|---|
| `Ability_SoA_Upgrade_Additional_Max_Attacks` | 60 | `SoAsUpgradeMaxAmountAttacks` | `SeriesOfAttacks` | SimpleUpgrade: один SimpleAbilityParameterDecorator по строке SeriesOfAttacks.Parameters.MaxAttacks, членов класса не читает |  |
| `Ability_SoA_Upgrade_More_Attack_Damage` | 66 | `SoAsUpgradeMoreAttackDamage` | `SeriesOfAttacks` | SimpleUpgrade: декоратор по ключу DamageMultiplier, только число |  |
| `Ability_SoA_Upgrade_Additional_Attacks` | 77 | `SoAsUpgradeAdditionalAttacks` | `SeriesOfAttacks` | Два декоратора (MinAttacks/MaxAttacks) через AddParameterDecorator базового Ability — только числа |  |
| `Ability_SoA_Upgrade_Reduce_Cost` | 83 | `AbilityUpgradeReduceCost (SimpleUpgrade<Ability>)` | — | Декоратор по общему ключу AbilityParameter.CostValue, T=Ability — работает на любой способности |  |
| `Ability_SoA_Upgrade_Reduce_Cooldown` | 89 | `AbilityUpgradeReduceCooldown (SimpleUpgrade<Ability>)` | — | Декоратор по общему ключу AbilityParameter.Cooldown, T=Ability — универсален |  |
| `Ability_Ip_Upgrade_Additional_Amount_Attacks` | 130 | `IpUpgradeAmountAttacks` | `IncreasingPressure` | SimpleUpgrade: один декоратор по ключу IncreasingPressure.Parameters.Attacks |  |
| `Ability_Ip_Upgrade_Additional_Damage_Multiplier` | 136 | `IpUpgradeAdditionalDamageMultiplier` | `IncreasingPressure` | SimpleUpgrade: декоратор по ключу AttackDamageStepMultiplier, только число |  |
| `Ability_Ip_Upgrade_Reduce_Cooldown` | 142 | `AbilityUpgradeReduceCooldown (SimpleUpgrade<Ability>)` | — | Общий ключ Cooldown, T=Ability — универсален |  |
| `Ability_JoP_Upgrade_Increasing_Scales` | 192 | `JoPUpgradeIncreasingScales` | `JarOfPoison` | Два декоратора по ОБЩИМ ключам WeaponDamageScale/SpellDamageScale — чистые числа, привязка только через параметр T |  |
| `Ability_JoP_Upgrade_Poison_Duration` | 199 | `JoPUpgradePoisonDuration` | `JarOfPoison` | SimpleUpgrade: декоратор по ключу JarOfPoison.Parameters.PoisonDuration |  |
| `Ability_JoP_Upgrade_Reduce_Cooldown` | 205 | `AbilityUpgradeReduceCooldown (SimpleUpgrade<Ability>)` | — | Общий ключ Cooldown, T=Ability |  |
| `Ability_Ov_Upgrade_Reduce_Cooldown` | 211 | `AbilityUpgradeReduceCooldown (SimpleUpgrade<Ability>)` | — | Общий ключ Cooldown, T=Ability |  |
| `Ability_Ov_Upgrade_Burn_Add_Cost` | 217 | `OvUpgradeBurnAddCost` | `Overload` | Два декоратора: Override по Overload.Parameters.ManaBurnPercent и Add по общему CostValue; членов Overload не читает |  |
| `Ability_Ov_Upgrade_Reduce_Cost` | 224 | `AbilityUpgradeReduceCost (SimpleUpgrade<Ability>)` | — | Общий ключ CostValue, T=Ability |  |
| `Ability_Ov_Upgrade_Mana_Step` | 230 | `SimpleUpgrade<Ability> + SimpleAbilityParameterDecorator(Overload.Parameters.ManaPerStep)` | `Overload` | Тип общий (SimpleUpgrade<Ability>), но строковый ключ ManaPerStep регистрирует ТОЛЬКО Overload — на чужой способности AddDecorator даёт Tracker.TrackNotFound и декоратор никто не читает | да |
| `Ability_Ov_Upgrade_Additional_Multiplier` | 238 | `SimpleUpgrade<Ability> + SimpleAbilityParameterDecorator(Overload.Parameters.DamagePerStep)` | `Overload` | Тот же случай: универсальный T=Ability при Overload-приватном ключе DamagePerStep — тихая инертность на чужой способности | да |
| `Ability_Cl_Upgrade_Reduce_Cooldown` | 276 | `AbilityUpgradeReduceCooldown (SimpleUpgrade<Ability>)` | — | Общий ключ Cooldown, T=Ability |  |
| `Ability_Cl_Upgrade_Scales_Add_Cost` | 282 | `ClUpgradeScalesAddCost` | `ChainLightning` | Три декоратора по общим ключам WeaponDamageScale/SpellDamageScale/CostValue; членов ChainLightning не касается |  |
| `Ability_Cl_Upgrade_Reduce_Cost` | 290 | `AbilityUpgradeReduceCost (SimpleUpgrade<Ability>)` | — | Общий ключ CostValue, T=Ability |  |
| `Ability_Cl_Upgrade_Additional_Jump` | 296 | `ClUpgradeParameter` | `ChainLightning` | Один декоратор Add по переданной строке (ChainLightning.Parameters.Jumps) — тело идентично общему параметр-апгрейду |  |
| `Ability_Cl_Upgrade_Reduce_Falloff` | 303 | `ClUpgradeParameter` | `ChainLightning` | Тот же класс, отрицательная дельта по ключу DamageFalloff — только число |  |
| `Ability_Ia_Upgrade_Reduce_Cost` | 317 | `AbilityUpgradeReduceCost (SimpleUpgrade<Ability>)` | — | Общий ключ CostValue, T=Ability |  |
| `Ability_Ia_Upgrade_Reduce_Cooldown` | 323 | `AbilityUpgradeReduceCooldown (SimpleUpgrade<Ability>)` | — | Общий ключ Cooldown, T=Ability |  |
| `Ability_Ia_Upgrade_Additional_Barrier` | 329 | `IaUpgradeParameter` | `IceAegis` | Один Add-декоратор по строке IceAegis.Parameters.BarrierBase; тело класса — копия общего параметр-апгрейда |  |
| `Ability_Ia_Upgrade_Additional_Scale` | 336 | `IaUpgradeParameter` | `IceAegis` | Add-декоратор по ключу PerIntelligenceScale — только число |  |
| `Ability_Ia_Upgrade_Additional_Duration` | 343 | `IaUpgradeParameter` | `IceAegis` | Add-декоратор по ключу Duration — только число |  |
| `Ability_Arm_Upgrade_Reduce_Cost` | 350 | `AbilityUpgradeReduceCost (SimpleUpgrade<Ability>)` | — | Общий ключ CostValue, T=Ability |  |
| `Ability_Arm_Upgrade_Reduce_Cooldown` | 356 | `AbilityUpgradeReduceCooldown (SimpleUpgrade<Ability>)` | — | Общий ключ Cooldown, T=Ability |  |
| `Ability_Arm_Upgrade_Extend_Stun` | 362 | `ArmUpgradeParameter` | `Armageddon` | Add-декоратор по строке Armageddon.Parameters.StunDuration; членов класса не читает |  |
| `Ability_Arm_Upgrade_Reduce_Hp_Cost` | 369 | `ArmUpgradeParameter` | `Armageddon` | Add-декоратор (отрицательная дельта) по ключу HpCostMultiplier — только число |  |
| `Ability_Arm_Upgrade_Stage1_Damage` | 376 | `ArmUpgradeStage1Override` | `Armageddon` | Три Override-декоратора по ОБЩИМ ключам Damage/WeaponDamageScale/SpellDamageScale — чистая подстановка чисел |  |
| `Ability_Arm_Upgrade_Missing_Hp_Damage` | 392 | `ArmUpgradeParameter` | `Armageddon` | Add-декоратор по ключу MissingHpRate — только число |  |
| `Ability_Porc_Upgrade_Reduce_Cooldown_Add_Cost` | 414 | `AbilityUpgradeReduceCooldownAddCost` | — | AbilityUpgrade<Ability>: два декоратора по общим ключам Cooldown и CostValue — универсален |  |
| `Ability_Porc_Upgrade_Cooldown_Chance` | 421 | `PorcUpgradeParameter` | `Porcupine` | Add-декоратор по строке Porcupine.Parameters.CooldownReduceChance; тело — копия общего параметр-апгрейда |  |
| `Ability_Porc_Upgrade_Heal_On_Hit` | 435 | `PorcUpgradeParameter` | `Porcupine` | Add-декоратор по ключу HealOnHit — только число |  |
| `Ability_Porc_Upgrade_More_Armor_Return` | 442 | `PorcUpgradeParameter` | `Porcupine` | Add-декоратор по ключу ArmorReturn — только число |  |
| `Ability_Porc_Upgrade_More_Damage_Return` | 449 | `PorcUpgradeParameter` | `Porcupine` | Add-декоратор по ключу DamageReturn — только число |  |
| `Ability_Sac_Upgrade_Additional_Charge` | 479 | `SacUpgradeParameter` | `Sacrifice` | тело класса — только SimpleAbilityParameterDecorator по строковому ключу через базовый Ability.AddParameterDecorator; членов Sacrifice не читает |  |
| `Ability_Sac_Upgrade_Reduce_Cooldown` | 486 | `AbilityUpgradeReduceCooldown` | — | SimpleUpgrade<Ability> с декоратором на AbilityParameter.Cooldown — ключ регистрирует RegisterBaseParameters у любой способности |  |
| `Ability_Sac_Upgrade_Reduce_Cost` | 492 | `AbilityUpgradeReduceCost` | — | SimpleUpgrade<Ability> с декоратором на AbilityParameter.CostValue — универсальный ключ базы |  |
| `Ability_Sac_Upgrade_Additional_Rate` | 498 | `SacUpgradeParameter` | `Sacrifice` | тот же decorator-only класс, меняется только строковый ключ параметра |  |
| `Ability_Sac_Upgrade_More_Sacrifice` | 505 | `SacUpgradeParameter` | `Sacrifice` | тот же decorator-only класс, меняется только строковый ключ параметра |  |
| `Ability_Sac_Upgrade_Heal_From_Damage` | 518 | `SacUpgradeParameter` | `Sacrifice` | тот же decorator-only класс, меняется только строковый ключ параметра |  |
| `Ability_Bf_Upgrade_Reduce_Cooldown` | 540 | `AbilityUpgradeReduceCooldown` | — | SimpleUpgrade<Ability>, декоратор на общий Cooldown |  |
| `Ability_Bf_Upgrade_Reduce_Cost` | 546 | `AbilityUpgradeReduceCost` | — | SimpleUpgrade<Ability>, декоратор на общий CostValue |  |
| `Ability_Bf_Upgrade_Fury_Duration` | 552 | `BfUpgradeFuryDuration` | `BerserkFury` | ApplyUpgrade — один Subtract-декоратор на строковый ключ BerserkFury.Parameters.FuryDuration, инстанс-членов способности нет |  |
| `Ability_Bf_Upgrade_More_Burn` | 564 | `BfUpgradeFuryBurn` | `BerserkFury` | один Add-декоратор на строковый ключ FuryHealthPercent, членов BerserkFury не читает |  |
| `Ability_Bf_Upgrade_Less_Burn` | 570 | `BfUpgradeFuryBurn` | `BerserkFury` | тот же decorator-only класс с отрицательной величиной |  |
| `Ability_Ar_Upgrade_Buff_Duration` | 605 | `ArUpgradeBuffDuration` | `AresBlessing` | один Add-декоратор на строковый ключ AresBlessing.Parameters.Duration, инстанс-членов не трогает |  |
| `Ability_Ar_Upgrade_Reduce_Cooldown` | 611 | `AbilityUpgradeReduceCooldown` | — | SimpleUpgrade<Ability>, декоратор на общий Cooldown |  |
| `Ability_Ar_Upgrade_Reduce_Cost` | 617 | `AbilityUpgradeReduceCost` | — | SimpleUpgrade<Ability>, декоратор на общий CostValue |  |
| `Ability_Ar_Upgrade_Recovery_Bonus` | 623 | `ArUpgradeBlessingBonus` | `AresBlessing` | два условных Add-декоратора по строковым ключам HealthBonus/RecoveryBonus, чтения членов способности нет |  |
| `Ability_Ar_Upgrade_Health_Bonus` | 630 | `ArUpgradeBlessingBonus` | `AresBlessing` | тот же decorator-only класс, второй бонус нулевой |  |
| `Ability_Ar_Upgrade_Both_Bonuses` | 637 | `ArUpgradeBlessingBonus` | `AresBlessing` | тот же decorator-only класс, оба бонуса ненулевые |  |
| `Ability_Dst_Upgrade_Reduce_Cost` | 665 | `AbilityUpgradeReduceCost` | — | SimpleUpgrade<Ability>, декоратор на общий CostValue |  |
| `Ability_Dst_Upgrade_Reduce_Cooldown` | 671 | `AbilityUpgradeReduceCooldown` | — | SimpleUpgrade<Ability>, декоратор на общий Cooldown |  |
| `Ability_Dst_Upgrade_Damage_Multiplier` | 683 | `DstUpgradeDamageMultiplier` | `DoubleStrike` | один Add-декоратор на строковый ключ DoubleStrike.Parameters.DamageMultiplier |  |
| `Ability_Dst_Upgrade_Restore_On_Hit` | 696 | `DstUpgradeRestoreOnHit` | `DoubleStrike` | два Add-декоратора по строковым ключам HealthRestore/ManaRestore, само поведение уже зашито в ExecuteInternal способности |  |
| `Ability_Hb_Upgrade_Reduce_Cost` | 703 | `AbilityUpgradeReduceCost` | — | SimpleUpgrade<Ability>, декоратор на общий CostValue |  |
| `Ability_Hb_Upgrade_Reduce_Cooldown` | 709 | `AbilityUpgradeReduceCooldown` | — | SimpleUpgrade<Ability>, декоратор на общий Cooldown |  |
| `Ability_Hb_Upgrade_Additional_Scales` | 715 | `AbilityUpgradeAdditionalScales` | `DamagingAbility` | AbilityUpgrade<Ability> с двумя Add-декораторами на WeaponDamageScale/SpellDamageScale — ключи регистрирует только RegisterDamageParameters | да |
| `Ability_Hb_Upgrade_Extend_Stun_Add_Cost` | 722 | `HbUpgradeExtendStunAddCost` | `HeadButt` | два Add-декоратора: HeadButt.Parameters.StunDuration и общий CostValue, членов способности не читает |  |
| `Ability_Hb_Upgrade_Additional_Lunges` | 738 | `HbUpgradeAdditionalLunges` | `HeadButt` | один Add-декоратор на строковый ключ HeadButt.Parameters.Attacks |  |
| `Ability_Cc_Upgrade_More_Stacks_More_Cost` | 785 | `CcUpgradeMoreStacksMoreCost` | `CriticalCalculation` | два Add-декоратора: CriticalCalculation.Parameters.Stacks и общий CostValue |  |
| `Ability_Cc_Upgrade_Reduce_Cooldown` | 792 | `AbilityUpgradeReduceCooldown` | — | SimpleUpgrade<Ability>, декоратор на общий Cooldown |  |
| `Ability_Cc_Upgrade_Reduce_Cost` | 798 | `AbilityUpgradeReduceCost` | — | SimpleUpgrade<Ability>, декоратор на общий CostValue |  |
| `Ability_Ds_Upgrade_Additional_Health_Regen` | 830 | `DsUpgradeAdditionalHealthRegen` | `DarkShroud` | SimpleUpgrade<DarkShroud> с одним Add-декоратором на строковый ключ HealthRegen, кода в классе нет вовсе |  |
| `Ability_Ds_Upgrade_Add_Effectiveness_Reduce_Stacks` | 836 | `DsUpgradeAddEffectivenessReduceStacks` | `DarkShroud` | два декоратора (Add на Effectiveness, Subtract на Stacks) по строковым ключам, чтения членов нет |  |
| `Ability_Ds_Upgrade_Increased_Buff_Duration` | 843 | `DsUpgradeIncreasedBuffDuration` | `DarkShroud` | SimpleUpgrade<DarkShroud> с одним Add-декоратором на строковый ключ Duration |  |
| `Ability_Ds_Upgrade_Reduce_Cooldown` | 849 | `AbilityUpgradeReduceCooldown` | — | SimpleUpgrade<Ability>, декоратор на общий Cooldown |  |
| `Ability_Ds_Upgrade_Reduce_Cost` | 855 | `AbilityUpgradeReduceCost` | — | SimpleUpgrade<Ability>, декоратор на общий CostValue |  |
| `Ability_Pe_Upgrade_Reduce_Execution_Trahsold` | 884 | `PeUpgradeLowerExecutionThreshold` | `PoisonExplosion` | SimpleUpgrade<PoisonExplosion> с одним Subtract-декоратором на строковый ключ ExecutionThreshold |  |
| `Ability_Pe_Upgrade_Reduce_Cost` | 895 | `AbilityUpgradeReduceCost` | — | SimpleUpgrade<Ability>, декоратор на общий CostValue |  |
| `Ability_Pe_Upgrade_Reduce_Cooldown` | 901 | `AbilityUpgradeReduceCooldownAddCost` | — | AbilityUpgrade<Ability>: два SimpleAbilityParameterDecorator по универсальным ключам Cooldown и CostValue (регистрируются в Ability.RegisterBaseParameters), членов способности не читает |  |
| `Ability_Pc_Upgrade_Reduce_Cooldown` | 961 | `AbilityUpgradeReduceCooldown` | — | SimpleUpgrade<Ability> с декоратором Subtract по универсальному AbilityParameter.Cooldown |  |
| `Ability_Pc_Upgrade_Reduce_Cost` | 967 | `AbilityUpgradeReduceCost` | — | SimpleUpgrade<Ability> с декоратором Subtract по универсальному AbilityParameter.CostValue |  |
| `Ability_Is_Upgrade_Reduce_Cooldown_Add_Cost` | 973 | `AbilityUpgradeReduceCooldownAddCost` | — | AbilityUpgrade<Ability>, два декоратора по универсальным Cooldown/CostValue |  |
| `Ability_Is_Upgrade_Additional_Scales` | 980 | `AbilityUpgradeAdditionalScales` | — | AbilityUpgrade<Ability>, декораторы по WeaponDamageScale/SpellDamageScale; ключи регистрирует только RegisterDamageParameters (DamagingAbility, IceShards, IceBlocks, ChainLightning, Discharge) — на прочих AddDecorator даст TrackNotFound и станет инертным | да |
| `Ability_Is_Upgrade_Reduce_Cost` | 987 | `AbilityUpgradeReduceCost` | — | декоратор по универсальному CostValue, членов способности не касается |  |
| `Ability_Is_Upgrade_Additional_Crit_Damage` | 993 | `AbilityUpgradeAdditionalCritDamage` | — | SimpleUpgrade<Ability> по AbilityParameter.CriticalDamageBonus; ключ регистрирует ТОЛЬКО MulticastAbility.RegisterBaseParameters — вне семейства мультикаста декоратор инертен | да |
| `Ability_Is_Upgrade_Additional_Crit_Chance` | 999 | `AbilityUpgradeAdditionalCritChance` | — | SimpleUpgrade<Ability> по AbilityParameter.CriticalChanceBonus; ключ регистрирует ТОЛЬКО MulticastAbility — вне семейства инертен | да |
| `Ability_Ib_Upgrade_Withering_Value` | 1061 | `SimpleUpgrade<Ability>` | — | чистый декоратор, но ключ IceBlocks.Parameters.WitheringValue регистрирует только IceBlocks — на другой способности AddDecorator даст TrackNotFound и апгрейд станет молча инертным | да |
| `Ability_Ib_Upgrade_Reduce_Cooldown` | 1069 | `AbilityUpgradeReduceCooldown` | — | декоратор Subtract по универсальному AbilityParameter.Cooldown |  |
| `Ability_Ib_Upgrade_Reduce_Cost` | 1075 | `AbilityUpgradeReduceCost` | — | декоратор Subtract по универсальному AbilityParameter.CostValue |  |
| `Ability_Ib_Upgrade_Withering_Stacks` | 1081 | `SimpleUpgrade<Ability>` | — | декоратор по IceBlocks.Parameters.WitheringMaxStacks: тип общий, ключ — только у IceBlocks, вне неё инертен | да |
| `Ability_Ib_Upgrade_Extra_Block_Damage` | 1089 | `SimpleUpgrade<Ability>` | — | декоратор по IceBlocks.Parameters.ExtraBlockDamagePercent — ключ регистрирует только IceBlocks | да |
| `Ability_Ib_Upgrade_Heavy_Blocks` | 1097 | `AbilityUpgradeParameterSet` | — | AbilityUpgrade<Ability>, три декоратора по Damage/WeaponDamageScale/SpellDamageScale — ключи ставит только RegisterDamageParameters (5 классов), на не-уронной способности набор инертен | да |
| `Ability_Df_Upgrade_Reduce_Cost` | 1128 | `AbilityUpgradeReduceCost` | — | декоратор по универсальному CostValue |  |
| `Ability_Df_Upgrade_Reduce_Cooldown` | 1134 | `AbilityUpgradeReduceCooldown` | — | декоратор по универсальному Cooldown |  |
| `Ability_Df_Upgrade_Frostbite_Duration` | 1140 | `SimpleUpgrade<Ability>` | — | декоратор по DeepFreeze.Parameters.FrostbiteDuration: тип общий, ключ регистрирует только DeepFreeze | да |
| `Ability_Df_Upgrade_More_Shred` | 1162 | `SimpleUpgrade<Ability>` | — | декоратор по DeepFreeze.Parameters.ColdResistanceShred — ключ есть только у DeepFreeze, вне неё инертен | да |
| `Ability_Dis_Upgrade_Reduce_Cost` | 1190 | `AbilityUpgradeReduceCost` | — | декоратор по универсальному CostValue |  |
| `Ability_Dis_Upgrade_Reduce_Cooldown` | 1196 | `AbilityUpgradeReduceCooldown` | — | декоратор по универсальному Cooldown |  |
| `Ability_Dis_Upgrade_Multiplier` | 1202 | `SimpleUpgrade<Ability>` | — | декоратор по Discharge.Parameters.BarrierMultiplier: тип общий, ключ регистрирует только Discharge | да |
| `Ability_Dis_Upgrade_More_Multiplier` | 1210 | `SimpleUpgrade<Ability>` | — | тот же ключ Discharge.Parameters.BarrierMultiplier с бОльшим значением — вне Discharge инертен | да |
| `Ability_Dis_Upgrade_More_Restore` | 1218 | `SimpleUpgrade<Ability>` | — | декоратор по Discharge.Parameters.StageThreeBarrierRestore — ключ только у Discharge | да |
| `Ability_Dis_Upgrade_Spell_Scale` | 1226 | `SimpleUpgrade<Ability>` | — | декоратор по общему AbilityParameter.SpellDamageScale, но ключ ставит только RegisterDamageParameters (5 классов) — на не-уронной способности инертен | да |
| `Ability_Sa_Upgrade_Reduce_Cost` | 1255 | `AbilityUpgradeReduceCost` | — | декоратор по универсальному CostValue |  |
| `Ability_Sa_Upgrade_Reduce_Cooldown` | 1261 | `AbilityUpgradeReduceCooldown` | — | декоратор по универсальному Cooldown |  |
| `Ability_Sa_Upgrade_Detonation_Scales` | 1267 | `AbilityUpgradeParameterSet` | — | обёртка типизирована Ability, но бампит StaticArmor.Parameters.DetonationWeaponScale/DetonationSpellScale — вне StaticArmor оба декоратора инертны (тот самый ParameterSet из F-35) | да |
| `Ability_Sa_Upgrade_Buff_Duration` | 1276 | `SimpleUpgrade<Ability>` | — | декоратор по StaticArmor.Parameters.Duration. Ключ Duration — литерал nameof, его регистрируют минимум ШЕСТЬ способностей: на чужой способности запись не инертна, а молча продлевает её собственный баф (W-91) | да |
| `Ability_Sa_Upgrade_More_Splash` | 1284 | `SimpleUpgrade<Ability>` | — | декоратор по StaticArmor.Parameters.StageThreeSplashDamage — ключ только у StaticArmor | да |
| `Ability_Sa_Upgrade_Less_Stacks` | 1292 | `SimpleUpgrade<Ability>` | — | декоратор Subtract по StaticArmor.Parameters.RequiredStacks — ключ только у StaticArmor, вне неё инертен | да |
| `Ability_Sa_Upgrade_Cost_Barrier` | 1314 | `AbilityUpgradeCostTypeOverride` | — | AbilityUpgrade<Ability>: Override-декоратор по универсальному AbilityParameter.CostType (enum хранится числом), членов способности не касается |  |

## Обобщаемая — 27

| Id | Стр. | Создаёт | Привязка | Почему | Скрытая |
|---|---|---|---|---|---|
| `Ability_SoA_Upgrade_Poison_On_Hit` | 36 | `SoAsUpgradePoisonOnHit` | `SeriesOfAttacks` | Тело трогает только Ability.ImpactRiders + общий PoisonOnHitRider; концерто-специфики нет, ограничение T=SeriesOfAttacks лишнее (точный дубль AbilityUpgradeImpactRider) |  |
| `Ability_SoA_Upgrade_Attacks_Cannot_Be_Evaded` | 72 | `SoAsUpgradeUnevadable (DelegateUpgrade<SeriesOfAttacks>)` | `SeriesOfAttacks` | Дёргает IsEvadable — член общего контракта IDamagingAbility/DamagingAbility, не SeriesOfAttacks; но флаг читает только SoAsDefaultExecutionStrategy | да |
| `Ability_Ip_Upgrade_Attack_Random_Target` | 100 | `IpUpgradeAttackRandomTarget` | `IncreasingPressure` | Только ability.ImpactRiders + общий SplashRandomTargetRider (работает над AbilityImpact/IBattleField) |  |
| `Ability_Ip_Upgrade_Attack_Extend_Poison` | 118 | `IpUpgradeExtendPoison` | `IncreasingPressure` | Только ability.ImpactRiders + общий ExtendPoisonOnHitRider (тянет эффекты цели по StatusEffects.Poison) |  |
| `Ability_JoP_Upgrade_Transfer_Poison_On_Death` | 154 | `JoPUpgradeTransferOnDeath` | `JarOfPoison` | Только ability.ImpactRiders + общий TransferPoisonOnDeathRider (подписка на EntityDiedEvent цели) |  |
| `Ability_JoP_Upgrade_Clumsiness` | 164 | `JoPDebuffUpgrade + ApplyEffectImpactRider(Clumsiness)` | `JarOfPoison` | Тело только ability.ImpactRiders; райдер и Clumsiness (ParameterChangeEffect на Evade) полностью общие — эквивалент AbilityUpgradeImpactRider |  |
| `Ability_JoP_Upgrade_Apply_Blind` | 174 | `JoPDebuffUpgrade + ApplyEffectImpactRider(BlindEffect)` | `JarOfPoison` | То же: ImpactRiders + общий эффект на EntityParameter.Accuracy |  |
| `Ability_JoP_Upgrade_Apply_Weakness` | 183 | `JoPDebuffUpgrade + ApplyEffectImpactRider(Weakness)` | `JarOfPoison` | То же: ImpactRiders + Weakness (HitDamageDealtContextModifier на цели), членов JarOfPoison не касается |  |
| `Ability_Ov_Upgrade_Random_Cooldown` | 246 | `AbilityUpgradeActivationRider + ReduceRandomCooldownActivationRider` | — | AbilityUpgrade<Ability>; райдер работает над IAbilityActivationContext (context.Caster.AbilityBook) — про Overload ничего не знает |  |
| `Ability_Ov_Upgrade_Mana_Flow` | 252 | `AbilityUpgradeCastEffect + ManaRegenerationEffect` | — | Фабрика игнорирует ability (лямбда `_ =>`), эффект общий (MaxMana/RestoreMana) — DeferredEffectActivationRider на любой Ability |  |
| `Ability_Ov_Upgrade_Next_Cast_Pure` | 262 | `AbilityUpgradeCastEffect + NextCastPureConversionEffect` | — | Фабрика берёт только ability.Id (член базового Ability); эффект вешает DamageConversionContextModifier на носителя — универсален |  |
| `Ability_Arm_Upgrade_Shatter_Armor` | 399 | `AbilityUpgradeImpactRider + ApplyEffectImpactRider(ArmorReductionEffect)` | — | AbilityUpgrade<Ability>: кладёт общий райдер в ImpactRiders, эффект — ParameterChangeEffect на EntityParameter.Armor; работает на любой способности с импактами |  |
| `Ability_Sac_Upgrade_Cost_Type_Health` | 512 | `AbilityUpgradeCostTypeOverride` | — | AbilityUpgrade<Ability>: Override-декоратор на AbilityParameter.CostType, ключ есть у всех способностей |  |
| `Ability_Sac_Upgrade_Incoming_Reduction` | 525 | `AbilityUpgradeCastEffect` | — | AbilityUpgrade<Ability> вешает DeferredEffectActivationRider; лямбда `_ =>` не смотрит на способность вообще, длительность из данных |  |
| `Ability_Sac_Upgrade_Free_Cast` | 534 | `AbilityUpgradeCastEffect` | — | лямбда читает только базовый Ability.Id; риск-контракт ActivationRiders прогоняется в Ability.Execute у любой способности |  |
| `Ability_Bf_Upgrade_Cost_Type_Health` | 558 | `AbilityUpgradeCostTypeOverride` | — | AbilityUpgrade<Ability>: Override-декоратор на общий CostType |  |
| `Ability_Hb_Upgrade_Armor_Debuff` | 729 | `AbilityUpgradeImpactRider + ApplyEffectImpactRider` | — | AbilityUpgrade<Ability> кладёт райдер в общий словарь ImpactRiders; сам райдер работает с AbilityImpact, а не со способностью | да |
| `Ability_Cc_Upgrade_Lucky_Crit` | 756 | `CcUpgradeLuckyCrit` | `CriticalCalculation` | тело — только ActivationRiders.TryAdd(AbilityBuffActivationRider) с эффектом из аргументов ctor; членов CriticalCalculation не читает, привязка чисто типовая |  |
| `Ability_Cc_Upgrade_Additional_Crit_Multiplier` | 762 | `CcUpgradeCritDamageBuff` | `CriticalCalculation` | AbilityBuffActivationRider из аргументов ctor кладётся в общий ActivationRiders, чтения способности нет |  |
| `Ability_Cc_Upgrade_Apply_Enhanced_Defence` | 770 | `CcUpgradeApplyEnhancedDefence` | `CriticalCalculation` | AbilityBuffActivationRider с EnhanceDefenseEffect из аргументов; только общий контракт ActivationRiders |  |
| `Ability_Cc_Upgrade_Leach_On_Crit` | 778 | `CcUpgradeLeachOnCrit` | `CriticalCalculation` | строит AbilityBuffActivationRider(CritLeechEffect) из аргументов в ApplyUpgrade и кладёт в общий ActivationRiders |  |
| `Ability_Ds_Upgrade_Immortality` | 816 | `DsUpgradeImmortality` | `DarkShroud` | тело — только ActivationRiders.TryAdd(AbilityBuffActivationRider) с LifeGivingShadeEffect из аргументов ctor |  |
| `Ability_Pe_Upgrade_Apply_Seal_Of_Oblivion` | 861 | `PeUpgradeApplySealOfOblivion` | `PoisonExplosion` | тело — только ActivationRiders.TryAdd(AbilityDebuffActivationRider) с OblivionSeal из аргументов; райдер бьёт по context.Targets, способность не читается |  |
| `Ability_Is_Upgrade_Apply_Fragility` | 1011 | `IsUpgradeApplyFragility` | — | наследник AbilityUpgradeImpactRider (типизирован Ability): вешает общий ApplyEffectImpactRider с общим FragilityEffect в Ability.ImpactRiders — весь код над контрактом IImpactRider | да |
| `Ability_Df_Upgrade_Enemy_Cooldown` | 1170 | `AbilityUpgradeImpactRider` | — | общая обёртка над Ability.ImpactRiders + общий ApplyEffectImpactRider с общим NextAbilityCooldownEffect — ни одного члена конкретной способности | да |
| `Ability_Df_Upgrade_Reduce_All_Cooldowns` | 1178 | `AbilityUpgradeActivationRider` | — | ReduceAllCooldownsActivationRider работает через IAbilityActivationContext (Caster.AbilityBook.AllAbilities, CooldownLeft) — чистый общий контракт, активационные райдеры вызывает базовый Ability.Execute для любой способности |  |
| `Ability_Df_Upgrade_Execute` | 1184 | `AbilityUpgradeImpactRider` | — | ExecuteImpactRider читает только AbilityImpact.Target (CurrentHealth/Parameters.MaxHealth/Kill) — поведение над IImpactRider | да |

## Именная — 58

| Id | Стр. | Создаёт | Привязка | Почему | Скрытая |
|---|---|---|---|---|---|
| `Ability_SoA_Upgrade_Apply_Buff_Critical_Chance` | 42 | `SoAsUpgradeApplyBuffCriticalChance` | `SeriesOfAttacks` | Читает и подменяет ability.ExecutionStrategy (ISoAExecutionStrategy) — собственную стратегию SeriesOfAttacks; SoAsApplyBuffStrategy наследует SoAsDefaultExecutionStrategy |  |
| `Ability_SoA_Upgrade_Apply_Buff_Critical_Damage` | 51 | `SoAsUpgradeApplyBuffCriticalDamage` | `SeriesOfAttacks` | Тот же свап ability.ExecutionStrategy на SoAsApplyBuffStrategy — член только SeriesOfAttacks |  |
| `Ability_Ip_Upgrade_Single_Empowered_Attack` | 95 | `IpUpgradeSingleEmpoweredAttack` | `IncreasingPressure` | Сохраняет и подменяет ability.ExecutionStrategy (IIpExecutionStrategy) — публичное поле только IncreasingPressure |  |
| `Ability_Ip_Upgrade_Last_Attack_Always_Crit` | 106 | `IpUpgradeLastAttackAlwaysCrit` | `IncreasingPressure` | Зовёт ability.AddAttackModifier/RemoveAttackModifier — метод объявлен на самом IncreasingPressure, общего интерфейса нет (дублируется в 4 классах) |  |
| `Ability_Ip_Upgrade_First_Attack_Crit_Damage` | 112 | `IpUpgradeFirstAttackCritDamage` | `IncreasingPressure` | Тот же ability.AddAttackModifier — член конкретного класса; сам FirstAttackCritContextModifier общий (IsFirst у IAttackContext) |  |
| `Ability_Ip_Upgrade_Unevadable` | 124 | `IpUpgradeUnevadable` | `IncreasingPressure` | ability.AddAttackModifier — член IncreasingPressure; сам UnevadableAttackContextModifier общий |  |
| `Ability_JoP_Upgrade_Bouncing` | 148 | `JoPUpgradeBouncing` | `JarOfPoison` | Читает и подменяет ability.HitSequence — свойство объявлено персонально на JarOfPoison (общего интерфейса доставки у Ability нет) |  |
| `Ability_JoP_Upgrade_All_Targets` | 159 | `JoPUpgradeAllTargets` | `JarOfPoison` | Свап ability.HitSequence на AllEnemiesHits — член конкретного JarOfPoison |  |
| `Ability_Ov_Upgrade_Stage4_Resets_Cooldown` | 269 | `DelegateUpgrade<Overload.Overload>` | `Overload` | Пишет ability.ResetCooldownOnFinalStage — собственное свойство Overload, читается в ExecutePlan |  |
| `Ability_Cl_Upgrade_Ignore_Resistances` | 310 | `DelegateUpgrade<ChainLightning.ChainLightning>` | `ChainLightning` | Пишет ability.IgnoreResistances — свойство самого ChainLightning, уезжает в ChainPlan/DamageContext |  |
| `Ability_Arm_Upgrade_Stage3_Burning` | 384 | `ArmUpgradeStage3Burning` | `Armageddon` | Пишет ability.Stage3EffectFactory и ability.Stage3EffectStacks — оба свойства объявлены только на Armageddon |  |
| `Ability_Arm_Upgrade_All_Targets` | 408 | `ArmUpgradeAllTargets` | `Armageddon` | Свап ability.HitSequence на AllEnemiesHits (плюс декоратор Cooldown) — HitSequence объявлен персонально на Armageddon |  |
| `Ability_Porc_Upgrade_Armor_Buff` | 428 | `AbilityUpgradeCastEffect + ArmorBuffEffect` | `Porcupine` | Обёртка общая (AbilityUpgrade<Ability>), но фабрика эффекта делает ((Porcupine)ability).Duration — на чужой способности InvalidCastException внутри DeferredEffectActivationRider на каждом касте | да |
| `Ability_Porc_Upgrade_Echo` | 456 | `AbilityUpgradeCastEffect + TemporarySkillEffect(EchoPassiveSkill)` | `Porcupine` | Тот же ((Porcupine)ability).Duration в фабрике общей обёртки — жёсткий каст, на чужой способности падает; сам TemporarySkillEffect/EchoPassiveSkill общие | да |
| `Ability_Porc_Upgrade_Incoming_Reduction` | 465 | `AbilityUpgradeCastEffect + IncomingDamageReductionEffect` | `Porcupine` | Фабрика снова кастует ((Porcupine)ability).Duration; эффект (IncomingDamageReductionContextModifier) сам по себе общий | да |
| `Ability_Porc_Upgrade_Crit_Mitigation` | 472 | `AbilityUpgradeCastEffect` | `Porcupine` | класс общий (AbilityUpgrade<Ability>, риск-контракт ActivationRiders), но лямбда записи жёстко кастит ((Porcupine)ability).Duration | да |
| `Ability_Bf_Upgrade_Burning_Fury` | 576 | `BfUpgradeFuryVariant` | `BerserkFury` | ApplyUpgrade подменяет собственную фабрику способности ability.FuryFactory и сохраняет прежнюю для отката |  |
| `Ability_Bf_Upgrade_Primal_Fury` | 587 | `BfUpgradeFuryVariant` | `BerserkFury` | подмена ability.FuryFactory — члена конкретного класса |  |
| `Ability_Bf_Upgrade_Healing_Fury` | 596 | `BfUpgradeFuryVariant` | `BerserkFury` | подмена ability.FuryFactory — члена конкретного класса |  |
| `Ability_Ar_Upgrade_Incoming_Reduction` | 644 | `ArUpgradeAdditionalCastEffect` | `AresBlessing` | фабрика типизирована Func<AresBlessing, IEffect> и лямбда читает ability.Duration — свойство конкретного класса |  |
| `Ability_Ar_Upgrade_Turn_End_Heal` | 651 | `ArUpgradeAdditionalCastEffect` | `AresBlessing` | лямбда читает ability.Duration конкретной AresBlessing |  |
| `Ability_Ar_Upgrade_Damage_Buff` | 658 | `ArUpgradeAdditionalCastEffect` | `AresBlessing` | лямбда читает ability.Duration конкретной AresBlessing |  |
| `Ability_Dst_Upgrade_Accuracy` | 677 | `DstUpgradeAccuracy` | `DoubleStrike` | зовёт ability.AddAttackModifier/RemoveAttackModifier — метод, объявленный в самом DoubleStrike (копипаста в BerserkFury/IncreasingPressure/SeriesOfAttacks, общего контракта нет) |  |
| `Ability_Dst_Upgrade_Both_Hits_Buff` | 689 | `DstUpgradeBothHitsBuff` | `DoubleStrike` | пишет ability.BothHitsBuffFactory — собственную точку расширения DoubleStrike |  |
| `Ability_Cc_Upgrade_Additional_Attack_Chance` | 744 | `CcUpgradeAdditionalAttackChance` | `CriticalCalculation` | подменяет ability.PrimaryBuffFactory — собственную фабрику баффа CriticalCalculation, с сохранением прежней |  |
| `Ability_Cc_Upgrade_Additional_Crit_Chance` | 750 | `CcUpgradeIncreaseCritChance` | `CriticalCalculation` | лямбда райдера читает ability.BuffDuration — свойство конкретного класса |  |
| `Ability_Ds_Upgrade_Additional_Attack_Chance` | 804 | `DsUpgradeAdditionalAttackChance` | `DarkShroud` | лямбда райдера читает ability.Duration конкретного DarkShroud |  |
| `Ability_Ds_Upgrade_Additional_Accuracy` | 810 | `DsUpgradeAdditionalAccuracy` | `DarkShroud` | лямбда читает ability.Duration и ability.Stacks — свойства DarkShroud |  |
| `Ability_Ds_Upgrade_Mana_Regen` | 824 | `DsUpgradeManaRegen` | `DarkShroud` | лямбда умножает на ability.Effectiveness и читает ability.Duration — свойства DarkShroud |  |
| `Ability_Pe_Upgrade_Execute_Bosses` | 868 | `PeUpgradeExecuteBosses` | `PoisonExplosion` | подменяет ability.ExecuteCondition и захватывает ability.ExecutionThreshold — обе точки живут в PoisonExplosion |  |
| `Ability_Pe_Upgrade_Spread_Poison` | 874 | `PeUpgradeSpreadPoison` | `PoisonExplosion` | подменяет собственную стратегию ability.SpreadMode на SpreadPoisonToAll |  |
| `Ability_Pe_Upgrade_Poison_Not_Removed` | 879 | `PeUpgradePreserveStacks` | `PoisonExplosion` | пишет булево поле ability.PreserveStacks конкретного класса |  |
| `Ability_Pe_Upgrade_Transfer_Poison_On_Death` | 890 | `PeUpgradeTransferPoisonOnDeath` | `PoisonExplosion` | подменяет ability.SpreadMode на SpreadPoisonToRandomTarget |  |
| `Ability_Pe_Upgrade_Total_Damage_Multiplier` | 908 | `PeUpgradeTotalDamageMultiplier` | `PoisonExplosion` | SimpleUpgrade<PoisonExplosion> — generic-параметр конкретный класс (Apply кинет TrackException/InvalidCastException на чужой), сам payload — чистый декоратор по PoisonExplosion.Parameters.DamageMultiplier |  |
| `Ability_Pc_Upgrade_Attacks_Reduce_Incoming_Heal` | 914 | `PcUpgradeApplyDebuffOnHit` | `PoisonCoating` | AbilityUpgrade<PoisonCoating>; внутри — общий AbilityDebuffActivationRider с общим HealReductionEffect, привязка идёт исключительно через generic-тип |  |
| `Ability_Pc_Upgrade_Attacks_Reduce_Armor` | 923 | `PcUpgradeApplyDebuffOnHit` | `PoisonCoating` | тот же AbilityUpgrade<PoisonCoating> с общим ArmorReductionEffect — код рабочий над IActivationRider, но тип прибит к PoisonCoating |  |
| `Ability_Pc_Upgrade_Apply_Stack_For_Each_Enemy` | 932 | `PcUpgradeMultiStackOnHit` | `PoisonCoating` | хранит ссылку на PoisonCoating и читает её собственные члены PoisonDuration/PoisonDamagePercent/InstanceId в обработчике AfterAttackEvent |  |
| `Ability_Pc_Upgrade_Increase_Poison_On_Target` | 937 | `PcUpgradeExtendExistingPoison` | `PoisonCoating` | AbilityUpgrade<PoisonCoating> с собственной подпиской на CombatEvents владельца; ApplyUpgrade пуст, поле _owner никогда не присваивается — апгрейд мёртв |  |
| `Ability_Pc_Upgrade_Additional_Poison_Stack_Duration` | 943 | `PcUpgradeAdditionalPoisonDuration` | `PoisonCoating` | SimpleUpgrade<PoisonCoating>: payload — чистый декоратор по общему `AbilityParameter.PoisonDuration`, но generic-тип конкретный |  |
| `Ability_Pc_Upgrade_Additional_Multiplier` | 949 | `PcUpgradeAdditionalMultiplier` | `PoisonCoating` | SimpleUpgrade<PoisonCoating>, декоратор по PoisonCoating.Parameters.PoisonMultiplier — числа плюс жёсткий generic-гейт |  |
| `Ability_Pc_Upgrade_Increase_Duration` | 955 | `PcUpgradeIncreaseDuration` | `PoisonCoating` | SimpleUpgrade<PoisonCoating>, декоратор по общему `AbilityParameter.Duration` (бывший PoisonCoating.Parameters.CoatingDuration) — числа плюс жёсткий generic-гейт |  |
| `Ability_Is_Upgrade_Barrier_From_Shrapnel` | 1005 | `IsUpgradeBarrierFromShrapnel` | `IceShards` | SimpleUpgrade<IceShards> с декоратором по IceShards.Parameters.ShrapnelBarrierLeach — generic-тип конкретный, ключ читает только IceShards.DealShrapnelBurst |  |
| `Ability_Is_Upgrade_Multicast` | 1019 | `DelegateUpgrade<IceShards>` | `IceShards` | лямбда пишет ability.Activation.BonusChance через конкретный тип IceShards; сам член живёт на базе семейства MulticastAbility, но запись переставима только внутри неё |  |
| `Ability_Is_Upgrade_Crit_Ignores_Cold_Res` | 1026 | `DelegateUpgrade<IceShards>` | `IceShards` | лямбда пишет собственное поле IceShards.CritIgnoresColdResistance, которое читает только CreateBasePlan этой способности |  |
| `Ability_Ia_Upgrade_Reflect` | 1033 | `IaUpgradeParameter` | `IceAegis` | AbilityUpgrade<IceAegis>: payload — чистый декоратор по переданному ключу (IceAegis.Parameters.ReflectPercent), привязка через generic-тип |  |
| `Ability_Ia_Upgrade_Stun_Attackers` | 1040 | `IaUpgradeParameter` | `IceAegis` | AbilityUpgrade<IceAegis>, декоратор по IceAegis.Parameters.StunAttackersChance (читается в CreateAttackerEffect этой способности) |  |
| `Ability_Ia_Upgrade_Turn_End_Heal` | 1047 | `IaUpgradeParameter` | `IceAegis` | AbilityUpgrade<IceAegis>, декоратор по IceAegis.Parameters.HealPerTurn — числа плюс жёсткий generic-гейт |  |
| `Ability_Ia_Upgrade_Crit_Mitigation` | 1054 | `AbilityUpgradeCastEffect` | `IceAegis` | обёртка типизирована базовым Ability и выглядит общей, но фабрика эффекта делает жёсткий каст ((IceAegis)ability).Duration — на чужой способности рантайм-InvalidCastException внутри лямбды (не мягкий Tracker-гейт AbilityUpgrade.Apply) | да |
| `Ability_Ib_Upgrade_Reset_Chance` | 1107 | `DelegateUpgrade<IceBlocks>` | `IceBlocks` | лямбда пишет собственное поле IceBlocks.ResetCooldownChance, читаемое в ExecutePlan этой способности |  |
| `Ability_Ib_Upgrade_Random_Extra_Blocks` | 1114 | `DelegateUpgrade<IceBlocks>` | `IceBlocks` | лямбда пишет IceBlocks.ExtraBlocksHitRandomTargets — флаг стадии 4 этой способности |  |
| `Ability_Ib_Upgrade_Consume_Stun` | 1121 | `DelegateUpgrade<IceBlocks>` | `IceBlocks` | лямбда пишет IceBlocks.ConsumeStunForDoubleDamage, который читает приватный TryConsumeStun этой способности |  |
| `Ability_Df_Upgrade_Spread_Freeze` | 1148 | `DelegateUpgrade<DeepFreeze>` | `DeepFreeze` | лямбда пишет собственное поле DeepFreeze.SpreadFreezeChance, читаемое приватным TrySpreadFreeze |  |
| `Ability_Df_Upgrade_Extend_Effects` | 1155 | `DelegateUpgrade<DeepFreeze>` | `DeepFreeze` | лямбда пишет DeepFreeze.ExtendTargetEffects — флаг, читаемый в ExecutePlan этой способности |  |
| `Ability_Dis_Upgrade_Overkill` | 1234 | `DelegateUpgrade<Discharge>` | `Discharge` | лямбда пишет собственное поле Discharge.OverkillToRandom, читаемое приватным TrySplashOverkill |  |
| `Ability_Dis_Upgrade_Ignore_Resistances` | 1241 | `DelegateUpgrade<Discharge>` | `Discharge` | лямбда пишет Discharge.AlwaysIgnoreResistances, которое попадает в план в CreateBasePlan этой способности |  |
| `Ability_Dis_Upgrade_Consume_Mana` | 1248 | `DelegateUpgrade<Discharge>` | `Discharge` | лямбда пишет Discharge.ConsumeManaInstead, читаемое приватным ConsumeResourcePool |  |
| `Ability_Sa_Upgrade_Overkill` | 1300 | `DelegateUpgrade<StaticArmor>` | `StaticArmor` | лямбда пишет собственное поле StaticArmor.OverkillToRandom, уезжающее в ChargeDetonation этой способности |  |
| `Ability_Sa_Upgrade_Ignore_Resistances` | 1307 | `DelegateUpgrade<StaticArmor>` | `StaticArmor` | лямбда пишет StaticArmor.IgnoreResistances, читаемое в ExecutePlan при сборке ChargeDetonation |  |

---

## Скрытые привязки — 30

Записи, у которых механизм отказа тихий. Механизма два: **ключа нет** — декоратор молча ничего не делает; **ключ есть у соседней способности** — срабатывает не по адресу. Каждой нужен явный `abilityId`, тегом не закрывается.

| Id | Корзина | Привязка | Почему тихо |
|---|---|---|---|
| `Ability_SoA_Upgrade_Attacks_Cannot_Be_Evaded` | Обобщаемая | `SeriesOfAttacks` | Дёргает IsEvadable — член общего контракта IDamagingAbility/DamagingAbility, не SeriesOfAttacks; но флаг читает только SoAsDefaultExecutionStrategy |
| `Ability_Ov_Upgrade_Mana_Step` | Числовая | `Overload` | Тип общий (SimpleUpgrade<Ability>), но строковый ключ ManaPerStep регистрирует ТОЛЬКО Overload — на чужой способности AddDecorator даёт Tracker.TrackNotFound и декоратор никто не читает |
| `Ability_Ov_Upgrade_Additional_Multiplier` | Числовая | `Overload` | Тот же случай: универсальный T=Ability при Overload-приватном ключе DamagePerStep — тихая инертность на чужой способности |
| `Ability_Porc_Upgrade_Armor_Buff` | Именная | `Porcupine` | Обёртка общая (AbilityUpgrade<Ability>), но фабрика эффекта делает ((Porcupine)ability).Duration — на чужой способности InvalidCastException внутри DeferredEffectActivationRider на каждом касте |
| `Ability_Porc_Upgrade_Echo` | Именная | `Porcupine` | Тот же ((Porcupine)ability).Duration в фабрике общей обёртки — жёсткий каст, на чужой способности падает; сам TemporarySkillEffect/EchoPassiveSkill общие |
| `Ability_Porc_Upgrade_Incoming_Reduction` | Именная | `Porcupine` | Фабрика снова кастует ((Porcupine)ability).Duration; эффект (IncomingDamageReductionContextModifier) сам по себе общий |
| `Ability_Porc_Upgrade_Crit_Mitigation` | Именная | `Porcupine` | класс общий (AbilityUpgrade<Ability>, риск-контракт ActivationRiders), но лямбда записи жёстко кастит ((Porcupine)ability).Duration |
| `Ability_Hb_Upgrade_Additional_Scales` | Числовая | `DamagingAbility` | AbilityUpgrade<Ability> с двумя Add-декораторами на WeaponDamageScale/SpellDamageScale — ключи регистрирует только RegisterDamageParameters |
| `Ability_Hb_Upgrade_Armor_Debuff` | Обобщаемая | — | AbilityUpgrade<Ability> кладёт райдер в общий словарь ImpactRiders; сам райдер работает с AbilityImpact, а не со способностью |
| `Ability_Is_Upgrade_Additional_Scales` | Числовая | — | AbilityUpgrade<Ability>, декораторы по WeaponDamageScale/SpellDamageScale; ключи регистрирует только RegisterDamageParameters (DamagingAbility, IceShards, IceBlocks, ChainLightning, Discharge) — на прочих AddDecorator даст TrackNotFound и станет инертным |
| `Ability_Is_Upgrade_Additional_Crit_Damage` | Числовая | — | SimpleUpgrade<Ability> по AbilityParameter.CriticalDamageBonus; ключ регистрирует ТОЛЬКО MulticastAbility.RegisterBaseParameters — вне семейства мультикаста декоратор инертен |
| `Ability_Is_Upgrade_Additional_Crit_Chance` | Числовая | — | SimpleUpgrade<Ability> по AbilityParameter.CriticalChanceBonus; ключ регистрирует ТОЛЬКО MulticastAbility — вне семейства инертен |
| `Ability_Is_Upgrade_Apply_Fragility` | Обобщаемая | — | наследник AbilityUpgradeImpactRider (типизирован Ability): вешает общий ApplyEffectImpactRider с общим FragilityEffect в Ability.ImpactRiders — весь код над контрактом IImpactRider |
| `Ability_Ia_Upgrade_Crit_Mitigation` | Именная | `IceAegis` | обёртка типизирована базовым Ability и выглядит общей, но фабрика эффекта делает жёсткий каст ((IceAegis)ability).Duration — на чужой способности рантайм-InvalidCastException внутри лямбды (не мягкий Tracker-гейт AbilityUpgrade.Apply) |
| `Ability_Ib_Upgrade_Withering_Value` | Числовая | — | чистый декоратор, но ключ IceBlocks.Parameters.WitheringValue регистрирует только IceBlocks — на другой способности AddDecorator даст TrackNotFound и апгрейд станет молча инертным |
| `Ability_Ib_Upgrade_Withering_Stacks` | Числовая | — | декоратор по IceBlocks.Parameters.WitheringMaxStacks: тип общий, ключ — только у IceBlocks, вне неё инертен |
| `Ability_Ib_Upgrade_Extra_Block_Damage` | Числовая | — | декоратор по IceBlocks.Parameters.ExtraBlockDamagePercent — ключ регистрирует только IceBlocks |
| `Ability_Ib_Upgrade_Heavy_Blocks` | Числовая | — | AbilityUpgrade<Ability>, три декоратора по Damage/WeaponDamageScale/SpellDamageScale — ключи ставит только RegisterDamageParameters (5 классов), на не-уронной способности набор инертен |
| `Ability_Df_Upgrade_Frostbite_Duration` | Числовая | — | декоратор по DeepFreeze.Parameters.FrostbiteDuration: тип общий, ключ регистрирует только DeepFreeze |
| `Ability_Df_Upgrade_More_Shred` | Числовая | — | декоратор по DeepFreeze.Parameters.ColdResistanceShred — ключ есть только у DeepFreeze, вне неё инертен |
| `Ability_Df_Upgrade_Enemy_Cooldown` | Обобщаемая | — | общая обёртка над Ability.ImpactRiders + общий ApplyEffectImpactRider с общим NextAbilityCooldownEffect — ни одного члена конкретной способности |
| `Ability_Df_Upgrade_Execute` | Обобщаемая | — | ExecuteImpactRider читает только AbilityImpact.Target (CurrentHealth/Parameters.MaxHealth/Kill) — поведение над IImpactRider |
| `Ability_Dis_Upgrade_Multiplier` | Числовая | — | декоратор по Discharge.Parameters.BarrierMultiplier: тип общий, ключ регистрирует только Discharge |
| `Ability_Dis_Upgrade_More_Multiplier` | Числовая | — | тот же ключ Discharge.Parameters.BarrierMultiplier с бОльшим значением — вне Discharge инертен |
| `Ability_Dis_Upgrade_More_Restore` | Числовая | — | декоратор по Discharge.Parameters.StageThreeBarrierRestore — ключ только у Discharge |
| `Ability_Dis_Upgrade_Spell_Scale` | Числовая | — | декоратор по общему AbilityParameter.SpellDamageScale, но ключ ставит только RegisterDamageParameters (5 классов) — на не-уронной способности инертен |
| `Ability_Sa_Upgrade_Detonation_Scales` | Числовая | — | обёртка типизирована Ability, но бампит StaticArmor.Parameters.DetonationWeaponScale/DetonationSpellScale — вне StaticArmor оба декоратора инертны (тот самый ParameterSet из F-35) |
| `Ability_Sa_Upgrade_Buff_Duration` | Числовая | — | декоратор по StaticArmor.Parameters.Duration. Ключ Duration — литерал nameof, его регистрируют минимум ШЕСТЬ способностей: на чужой способности запись не инертна, а молча продлевает её собственный баф (W-91) |
| `Ability_Sa_Upgrade_More_Splash` | Числовая | — | декоратор по StaticArmor.Parameters.StageThreeSplashDamage — ключ только у StaticArmor |
| `Ability_Sa_Upgrade_Less_Stacks` | Числовая | — | декоратор Subtract по StaticArmor.Parameters.RequiredStacks — ключ только у StaticArmor, вне неё инертен |

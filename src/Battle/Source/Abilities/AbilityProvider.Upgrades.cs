namespace Battle.Source.Abilities
{
    using System;
    using System.Collections.Generic;
    using Core.Data.AbilityData;
    using Core.Enums;
    using CriticalCalculation;
    using DarkShroud;
    using AresBlessing;
    using Armageddon;
    using BerserkFury;
    using DoubleStrike;
    using Effects;
    using HeadButt;
    using IncreasingPressure;
    using JarOfPoison;
    using PoisonCoating;
    using PoisonExplosion;
    using PassiveSkills;
    using Porcupine;
    using Riders;
    using Sacrifice;
    using SeriesOfAttacks;
    using ChainLightning;
    using Core.Battle.Abilities;
    using Core.Modifiers.Context;
    using IceAegis;
    using IceBlock;
    using IceShards;
    using Overload;

    public partial class AbilityProvider
    {
        private readonly Dictionary<string, Func<AbilityUpgradeData, IAbilityUpgrade>> _abilityUpgrades = new()
        {
            // Augments of the base contract every ability honours. They belong to no ability, so they
            // are written once — and those that move a number state it as a share of the number they
            // move, the only figure that means the same thing on a free cast and on a five-hundred one.
            ["Augment_Reduce_Cost"] = data =>
                new AbilityUpgradeReduceCost(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("costShare", 0.3f)),
            ["Augment_Reduce_Cooldown"] = data =>
                new AbilityUpgradeReduceCooldown(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("cooldownShare", 0.25f)),
            ["Augment_Reduce_Cooldown_Add_Cost"] = data =>
                new AbilityUpgradeReduceCooldownAddCost(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("cooldownShare", 0.4f),
                    data.UpgradeProperties.GetValueOrDefault("costShare", 0.4f)),
            ["Augment_Cost_Type_Health"] = data =>
                new AbilityUpgradeCostTypeOverride(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    Costs.Health),
            ["Augment_Poison_On_Hit"] = data =>
                new SoAsUpgradePoisonOnHit(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    (int)data.UpgradeProperties.GetValueOrDefault("poisonDuration", 3)),
            ["Ability_SoA_Augment_Apply_Buff_Critical_Chance"] = data =>
                new SoAsUpgradeApplyBuffCriticalChance(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    (int)data.UpgradeProperties.GetValueOrDefault("amountAttacks", 6),
                    data.UpgradeProperties.GetValueOrDefault("criticalChance", 0.15f),
                    (int)data.UpgradeProperties.GetValueOrDefault("duration", 5),
                    (int)data.UpgradeProperties.GetValueOrDefault("maxStacks", 3f)),
            ["Ability_SoA_Augment_Apply_Buff_Critical_Damage"] = data =>
                new SoAsUpgradeApplyBuffCriticalDamage(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    (int)data.UpgradeProperties.GetValueOrDefault("amountAttacks", 9),
                    data.UpgradeProperties.GetValueOrDefault("criticalDamage", 0.25f),
                    (int)data.UpgradeProperties.GetValueOrDefault("duration", 5),
                    (int)data.UpgradeProperties.GetValueOrDefault("maxStacks", 3)),
            ["Augment_Additional_Max_Attacks"] = data =>
                new SoAsUpgradeMaxAmountAttacks(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    (int)data.UpgradeProperties.GetValueOrDefault("maxAdditionalAttacks", 3)),
            ["Augment_More_Attack_Damage"] = data =>
                new SoAsUpgradeMoreAttackDamage(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("damageMultiplier", 0.15f)),
            ["Ability_SoA_Augment_Attacks_Cannot_Be_Evaded"] = data =>
                new SoAsUpgradeUnevadable(
                    data.Id,
                    data.Tags,
                    data.Tier),
            ["Augment_Additional_Attacks"] = data =>
                new SoAsUpgradeAdditionalAttacks(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    (int)data.UpgradeProperties.GetValueOrDefault("amountAttacks", 1)),
            ["Ability_Ip_Augment_Single_Empowered_Attack"] = data =>
                new IpUpgradeSingleEmpoweredAttack(
                    data.Id,
                    data.Tags,
                    data.Tier),
            ["Augment_Attack_Random_Target"] = data =>
                new IpUpgradeAttackRandomTarget(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("splashDamage", 0.45f)),
            ["Ability_Ip_Augment_Last_Attack_Always_Crit"] = data =>
                new IpUpgradeLastAttackAlwaysCrit(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    new LastAttackAlwaysCritContextModifier()),
            ["Ability_Ip_Augment_First_Attack_Crit_Damage"] = data =>
                new IpUpgradeFirstAttackCritDamage(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    new FirstAttackCritContextModifier(data.UpgradeProperties.GetValueOrDefault("critDamageBonus", 1.3f))),
            ["Augment_Attack_Extend_Poison"] = data =>
                new IpUpgradeExtendPoison(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    (int)data.UpgradeProperties.GetValueOrDefault("poisonDuration", 1)),
            ["Ability_Ip_Augment_Unevadable"] = data =>
                new IpUpgradeUnevadable(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    new UnevadableAttackContextModifier()),
            ["Augment_Additional_Amount_Attacks"] = data =>
                new IpUpgradeAmountAttacks(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    (int)data.UpgradeProperties.GetValueOrDefault("amountAttacks", 2)),
            ["Augment_Additional_Damage_Multiplier"] = data =>
                new IpUpgradeAdditionalDamageMultiplier(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("damageMultiplier", 0.05f)),
            ["Ability_JoP_Augment_Bouncing"] = data =>
                new JoPUpgradeBouncing(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    (int)data.UpgradeProperties.GetValueOrDefault("bounces", 5)),
            ["Augment_Transfer_Poison_On_Death"] = data =>
                new JoPUpgradeTransferOnDeath(
                    data.Id,
                    data.Tags,
                    data.Tier),
            ["Ability_JoP_Augment_All_Targets"] = data =>
                new JoPUpgradeAllTargets(
                    data.Id,
                    data.Tags,
                    data.Tier),
            ["Augment_Clumsiness"] = data =>
                new JoPDebuffUpgrade(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    new ApplyEffectImpactRider(
                        new Clumsiness(
                            (int)data.UpgradeProperties.GetValueOrDefault("duration", 5),
                            (int)data.UpgradeProperties.GetValueOrDefault("maxStacks", 5),
                            data.UpgradeProperties.GetValueOrDefault("evadeReduce", 0.05f)))),
            ["Augment_Apply_Blind"] = data =>
                new JoPDebuffUpgrade(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    new ApplyEffectImpactRider(new BlindEffect(
                        (int)data.UpgradeProperties.GetValueOrDefault("duration", 5),
                        (int)data.UpgradeProperties.GetValueOrDefault("maxStacks", 5),
                        data.UpgradeProperties.GetValueOrDefault("accuracyReduce", 0.15f)))),
            ["Augment_Apply_Weakness"] = data =>
                new JoPDebuffUpgrade(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    new ApplyEffectImpactRider(new Weakness(
                        (int)data.UpgradeProperties.GetValueOrDefault("duration", 5),
                        (int)data.UpgradeProperties.GetValueOrDefault("maxStacks", 5),
                        data.UpgradeProperties.GetValueOrDefault("damageReduce", 0.15f)))),
            ["Augment_Increasing_Scales"] = data =>
                new JoPUpgradeIncreasingScales(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("weaponDamageScale", 0.6f),
                    data.UpgradeProperties.GetValueOrDefault("spellDamageScale", 0.85f)),
            ["Augment_Poison_Duration"] = data =>
                new JoPUpgradePoisonDuration(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("poisonDuration", 1)),
            ["Augment_Burn_Add_Cost"] = data =>
                new OvUpgradeBurnAddCost(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("burnPercent", 0.30f),
                    data.UpgradeProperties.GetValueOrDefault("additionalCost", 50)),
            ["Ability_Ov_Augment_Mana_Step"] = data =>
                new SimpleUpgrade<Ability>(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    new SimpleAbilityParameterDecorator(
                        Overload.Overload.Parameters.ManaPerStep, Priority.Weak, OperationType.Subtract,
                        data.UpgradeProperties.GetValueOrDefault("amount", 1.5f), $"Ability_Parameter_Decorator_{data.Id}", data.Id)),
            ["Ability_Ov_Augment_Additional_Multiplier"] = data =>
                new SimpleUpgrade<Ability>(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    new SimpleAbilityParameterDecorator(
                        Overload.Overload.Parameters.DamagePerStep, Priority.Weak, OperationType.Add,
                        data.UpgradeProperties.GetValueOrDefault("amount", 0.02f), $"Ability_Parameter_Decorator_{data.Id}", data.Id)),
            ["Augment_Random_Cooldown"] = data =>
                new AbilityUpgradeActivationRider(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    new ReduceRandomCooldownActivationRider(data.Id, (int)data.UpgradeProperties.GetValueOrDefault("amount", 1))),
            ["Augment_Mana_Flow"] = data =>
                new AbilityUpgradeCastEffect(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    _ => new ManaRegenerationEffect(
                        data.UpgradeProperties.GetValueOrDefault("regenAmount", 0.15f),
                        (int)data.UpgradeProperties.GetValueOrDefault("duration", 3),
                        maxStacks: 1,
                        id: "Effect_Mana_Flow")),
            ["Augment_Next_Cast_Pure"] = data =>
                new AbilityUpgradeCastEffect(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    ability => new NextCastPureConversionEffect(ability.Id,
                        data.UpgradeProperties.GetValueOrDefault("fraction", 0.3f))),
            ["Ability_Ov_Augment_Stage4_Resets_Cooldown"] = data =>
                new DelegateUpgrade<Overload.Overload>(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    ability => ability.ResetCooldownOnFinalStage = true,
                    ability => ability.ResetCooldownOnFinalStage = false),
            ["Augment_Scales_Add_Cost"] = data =>
                new ClUpgradeScalesAddCost(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("weaponDamageScale", 0.15f),
                    data.UpgradeProperties.GetValueOrDefault("spellDamageScale", 0.35f),
                    data.UpgradeProperties.GetValueOrDefault("additionalCost", 50)),
            ["Augment_Additional_Jump"] = data =>
                new ClUpgradeParameter(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    ChainLightning.ChainLightning.Parameters.Jumps,
                    data.UpgradeProperties.GetValueOrDefault("amount", 1)),
            ["Augment_Reduce_Falloff"] = data =>
                new ClUpgradeParameter(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    ChainLightning.ChainLightning.Parameters.DamageFalloff,
                    -data.UpgradeProperties.GetValueOrDefault("amount", 0.10f)),
            ["Ability_Cl_Augment_Ignore_Resistances"] = data =>
                new DelegateUpgrade<ChainLightning.ChainLightning>(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    ability => ability.IgnoreResistances = true,
                    ability => ability.IgnoreResistances = false),
            ["Augment_Additional_Barrier"] = data =>
                new IaUpgradeParameter(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    IceAegis.IceAegis.Parameters.BarrierBase,
                    data.UpgradeProperties.GetValueOrDefault("amount", 300f)),
            ["Augment_Additional_Scale"] = data =>
                new IaUpgradeParameter(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    IceAegis.IceAegis.Parameters.PerIntelligenceScale,
                    data.UpgradeProperties.GetValueOrDefault("amount", 5f)),
            ["Augment_Additional_Duration"] = data =>
                new IaUpgradeParameter(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    IceAegis.IceAegis.Parameters.Duration,
                    data.UpgradeProperties.GetValueOrDefault("amount", 1f)),
            ["Augment_Extend_Stun"] = data =>
                new ArmUpgradeParameter(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    Armageddon.Armageddon.Parameters.StunDuration,
                    data.UpgradeProperties.GetValueOrDefault("duration", 1)),
            ["Augment_Reduce_Hp_Cost"] = data =>
                new ArmUpgradeParameter(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    Armageddon.Armageddon.Parameters.HpCostMultiplier,
                    -data.UpgradeProperties.GetValueOrDefault("amount", 0.15f)),
            ["Augment_Stage1_Damage"] = data =>
                new ArmUpgradeStage1Override(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("damage", 400f),
                    data.UpgradeProperties.GetValueOrDefault("weaponScale", 1f),
                    data.UpgradeProperties.GetValueOrDefault("spellScale", 1f)),
            ["Ability_Arm_Augment_Stage3_Burning"] = data =>
                new ArmUpgradeStage3Burning(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    (int)data.UpgradeProperties.GetValueOrDefault("stacks", 3),
                    (int)data.UpgradeProperties.GetValueOrDefault("duration", 3),
                    data.UpgradeProperties.GetValueOrDefault("damageMultiplier", 0.7f)),
            ["Augment_Missing_Hp_Damage"] = data =>
                new ArmUpgradeParameter(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    Armageddon.Armageddon.Parameters.MissingHpRate,
                    data.UpgradeProperties.GetValueOrDefault("rate", 1f)),
            ["Augment_Shatter_Armor"] = data =>
                new AbilityUpgradeImpactRider(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    new ApplyEffectImpactRider(new ArmorReductionEffect(
                        (int)data.UpgradeProperties.GetValueOrDefault("duration", 3),
                        maxStacks: 1,
                        data.UpgradeProperties.GetValueOrDefault("reduceBy", 1f)))),
            ["Ability_Arm_Augment_All_Targets"] = data =>
                new ArmUpgradeAllTargets(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("additionalCooldown", 3)),
            ["Augment_Cooldown_Chance"] = data =>
                new PorcUpgradeParameter(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    Porcupine.Porcupine.Parameters.CooldownReduceChance,
                    data.UpgradeProperties.GetValueOrDefault("chance", 0.15f)),
            ["Ability_Porc_Augment_Armor_Buff"] = data =>
                new AbilityUpgradeCastEffect(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    ability => new ArmorBuffEffect(((Porcupine.Porcupine)ability).Duration, maxStacks: 1,
                        data.UpgradeProperties.GetValueOrDefault("amount", 0.25f))),
            ["Augment_Heal_On_Hit"] = data =>
                new PorcUpgradeParameter(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    Porcupine.Porcupine.Parameters.HealOnHit,
                    data.UpgradeProperties.GetValueOrDefault("amount", 0.07f)),
            ["Augment_More_Armor_Return"] = data =>
                new PorcUpgradeParameter(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    Porcupine.Porcupine.Parameters.ArmorReturn,
                    data.UpgradeProperties.GetValueOrDefault("amount", 0.15f)),
            ["Augment_More_Damage_Return"] = data =>
                new PorcUpgradeParameter(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    Porcupine.Porcupine.Parameters.DamageReturn,
                    data.UpgradeProperties.GetValueOrDefault("amount", 0.20f)),
            ["Ability_Porc_Augment_Echo"] = data =>
                new AbilityUpgradeCastEffect(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    ability => new TemporarySkillEffect("Effect_Echo", ((Porcupine.Porcupine)ability).Duration,
                        new EchoPassiveSkill(
                            data.UpgradeProperties.GetValueOrDefault("delayedPercent", 0.3f),
                            (int)data.UpgradeProperties.GetValueOrDefault("turns", 2)))),
            ["Ability_Porc_Augment_Incoming_Reduction"] = data =>
                new AbilityUpgradeCastEffect(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    ability => new IncomingDamageReductionEffect(((Porcupine.Porcupine)ability).Duration, maxStacks: 1,
                        data.UpgradeProperties.GetValueOrDefault("reduce", 0.25f))),
            ["Ability_Porc_Augment_Crit_Mitigation"] = data =>
                new AbilityUpgradeCastEffect(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    ability => new EnhanceDefenseEffect(((Porcupine.Porcupine)ability).Duration, maxStacks: 1,
                        data.UpgradeProperties.GetValueOrDefault("amount", 0.8f))),
            ["Augment_Additional_Charge"] = data =>
                new SacUpgradeParameter(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    Sacrifice.Sacrifice.Parameters.Charges,
                    data.UpgradeProperties.GetValueOrDefault("amount", 1)),
            ["Augment_Additional_Rate"] = data =>
                new SacUpgradeParameter(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    Sacrifice.Sacrifice.Parameters.RatePerHundred,
                    data.UpgradeProperties.GetValueOrDefault("amount", 0.015f)),
            ["Augment_More_Sacrifice"] = data =>
                new SacUpgradeParameter(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    Sacrifice.Sacrifice.Parameters.SacrificePercent,
                    data.UpgradeProperties.GetValueOrDefault("amount", 0.10f)),
            ["Augment_Heal_From_Damage"] = data =>
                new SacUpgradeParameter(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    Sacrifice.Sacrifice.Parameters.HealPercent,
                    data.UpgradeProperties.GetValueOrDefault("amount", 0.15f)),
            ["Augment_Incoming_Reduction"] = data =>
                new AbilityUpgradeCastEffect(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    _ => new IncomingDamageReductionEffect(
                        (int)data.UpgradeProperties.GetValueOrDefault("duration", 3),
                        maxStacks: 1,
                        data.UpgradeProperties.GetValueOrDefault("reduce", 0.25f))),
            ["Augment_Free_Cast"] = data =>
                new AbilityUpgradeCastEffect(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    ability => new FreeCastEffect(ability.Id)),
            ["Augment_Fury_Duration"] = data =>
                new BfUpgradeFuryDuration(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("duration", 1)),
            ["Augment_More_Burn"] = data =>
                new BfUpgradeFuryBurn(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("amount", 0.035f)),
            ["Augment_Less_Burn"] = data =>
                new BfUpgradeFuryBurn(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    -data.UpgradeProperties.GetValueOrDefault("amount", 0.02f)),
            ["Ability_Bf_Augment_Burning_Fury"] = data =>
                new BfUpgradeFuryVariant(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    (duration, healthPercent) => new BurningFuryEffect(duration, maxStacks: 1, healthPercent)
                    {
                        BurnDamage = data.UpgradeProperties.GetValueOrDefault("healthAsDamageMultiplier", 1f),
                        BurningDuration = (int)data.UpgradeProperties.GetValueOrDefault("burningDuration", 3),
                        BurningMaxStacks = (int)data.UpgradeProperties.GetValueOrDefault("burningMaxStacks", 3)
                    }),
            ["Ability_Bf_Augment_Primal_Fury"] = data =>
                new BfUpgradeFuryVariant(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    (duration, healthPercent) => new PrimalFuryEffect(duration, maxMaxStacks: 1, healthPercent)
                    {
                        DamageMultiplier = data.UpgradeProperties.GetValueOrDefault("damageMultiplier", 1.5f)
                    }),
            ["Ability_Bf_Augment_Healing_Fury"] = data =>
                new BfUpgradeFuryVariant(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    (duration, healthPercent) => new HealingFuryEffect(duration, maxStacks: 1, healthPercent)
                    {
                        HealAmount = data.UpgradeProperties.GetValueOrDefault("healAmount", 0.5f)
                    }),
            ["Augment_Buff_Duration"] = data =>
                new ArUpgradeBuffDuration(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("duration", 1)),
            ["Augment_Recovery_Bonus"] = data =>
                new ArUpgradeBlessingBonus(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    healthBonus: 0f,
                    data.UpgradeProperties.GetValueOrDefault("recoveryBonus", 0.15f)),
            ["Augment_Health_Bonus"] = data =>
                new ArUpgradeBlessingBonus(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("healthBonus", 0.15f),
                    recoveryBonus: 0f),
            ["Augment_Both_Bonuses"] = data =>
                new ArUpgradeBlessingBonus(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("healthBonus", 0.07f),
                    data.UpgradeProperties.GetValueOrDefault("recoveryBonus", 0.07f)),
            ["Ability_Ar_Augment_Incoming_Reduction"] = data =>
                new ArUpgradeAdditionalCastEffect(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    ability => new IncomingDamageReductionEffect(ability.Duration, maxStacks: 1,
                        data.UpgradeProperties.GetValueOrDefault("reduce", 0.25f))),
            ["Ability_Ar_Augment_Turn_End_Heal"] = data =>
                new ArUpgradeAdditionalCastEffect(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    ability => new HealthRegenerationEffect(
                        data.UpgradeProperties.GetValueOrDefault("regenAmount", 0.08f), ability.Duration, maxStacks: 1)),
            ["Ability_Ar_Augment_Damage_Buff"] = data =>
                new ArUpgradeAdditionalCastEffect(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    ability => new DamageBuffEffect(ability.Duration, maxStacks: 1,
                        data.UpgradeProperties.GetValueOrDefault("amount", 0.55f))),
            ["Ability_Dst_Augment_Accuracy"] = data =>
                new DstUpgradeAccuracy(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("amount", 0.15f)),
            ["Augment_Damage_Multiplier"] = data =>
                new DstUpgradeDamageMultiplier(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("amount", 0.25f)),
            ["Ability_Dst_Augment_Both_Hits_Buff"] = data =>
                new DstUpgradeBothHitsBuff(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("amount", 0.25f),
                    (int)data.UpgradeProperties.GetValueOrDefault("duration", 3)),
            ["Augment_Restore_On_Hit"] = data =>
                new DstUpgradeRestoreOnHit(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("healthRestore", 0.07f),
                    data.UpgradeProperties.GetValueOrDefault("manaRestore", 0.07f)),
            ["Ability_Hb_Augment_Additional_Scales"] = data =>
                new AbilityUpgradeAdditionalScales(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("weaponDamageScale", 0.15f),
                    data.UpgradeProperties.GetValueOrDefault("spellDamageScale", 0.15f)),
            ["Augment_Extend_Stun_Add_Cost"] = data =>
                new HbUpgradeExtendStunAddCost(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("stunDuration", 1),
                    data.UpgradeProperties.GetValueOrDefault("additionalCost", 50)),
            ["Ability_Hb_Augment_Armor_Debuff"] = data =>
                new AbilityUpgradeImpactRider(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    new ApplyEffectImpactRider(new ArmorReductionEffect(
                        (int)data.UpgradeProperties.GetValueOrDefault("duration", 3),
                        (int)data.UpgradeProperties.GetValueOrDefault("maxStacks", 3),
                        data.UpgradeProperties.GetValueOrDefault("reduceArmorBy", 0.25f)))),
            ["Augment_Additional_Lunges"] = data =>
                new HbUpgradeAdditionalLunges(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    (int)data.UpgradeProperties.GetValueOrDefault("amount", 1)),
            ["Ability_Cc_Augment_Additional_Attack_Chance"] = data =>
                new CcUpgradeAdditionalAttackChance(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("value", 0.15f)),
            ["Ability_Cc_Augment_Additional_Crit_Chance"] = data =>
                new CcUpgradeIncreaseCritChance(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("criticalChance", 0.08f)),
            ["Augment_Lucky_Crit"] = data =>
                new CcUpgradeLuckyCrit(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    (int)data.UpgradeProperties.GetValueOrDefault("duration", 3)),
            ["Augment_Additional_Crit_Multiplier"] = data =>
                new CcUpgradeCritDamageBuff(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("multiplier", 0.15f),
                    data.UpgradeProperties.GetValueOrDefault("critChancePerHit", 0.15f),
                    (int)data.UpgradeProperties.GetValueOrDefault("duration", 3)),
            ["Augment_Apply_Enhanced_Defence"] = data =>
                new CcUpgradeApplyEnhancedDefence(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    (int)data.UpgradeProperties.GetValueOrDefault("duration", 3),
                    (int)data.UpgradeProperties.GetValueOrDefault("stacks", 3),
                    data.UpgradeProperties.GetValueOrDefault("value", 0.15f)),
            ["Augment_Leach_On_Crit"] = data =>
                new CcUpgradeLeachOnCrit(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("amount", 0.15f),
                    (int)data.UpgradeProperties.GetValueOrDefault("duration", 3)),
            ["Augment_More_Stacks_More_Cost"] = data =>
                new CcUpgradeMoreStacksMoreCost(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    (int)data.UpgradeProperties.GetValueOrDefault("stacks", 1),
                    data.UpgradeProperties.GetValueOrDefault("cost", 50)),
            ["Ability_Ds_Augment_Additional_Attack_Chance"] = data =>
                new DsUpgradeAdditionalAttackChance(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("amount", 0.15f)),
            ["Ability_Ds_Augment_Additional_Accuracy"] = data =>
                new DsUpgradeAdditionalAccuracy(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("amount", 0.2f)),
            ["Augment_Immortality"] = data =>
                new DsUpgradeImmortality(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("lifeToRecover", 0.35f),
                    (int)data.UpgradeProperties.GetValueOrDefault("duration", 3),
                    (int)data.UpgradeProperties.GetValueOrDefault("stacks", 1)),
            ["Ability_Ds_Augment_Mana_Regen"] = data =>
                new DsUpgradeManaRegen(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("regenAmount", 0.05f)),
            ["Augment_Additional_Health_Regen"] = data =>
                new DsUpgradeAdditionalHealthRegen(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("additionalRegen", 0.025f)),
            ["Augment_Add_Effectiveness_Reduce_Stacks"] = data =>
                new DsUpgradeAddEffectivenessReduceStacks(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("additionalEffectiveness", 0.35f),
                    (int)data.UpgradeProperties.GetValueOrDefault("amountStacks", 2)),
            ["Augment_Increased_Buff_Duration"] = data =>
                new DsUpgradeIncreasedBuffDuration(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("duration", 1)),
            ["Augment_Apply_Seal_Of_Oblivion"] = data =>
                new PeUpgradeApplySealOfOblivion(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    (int)data.UpgradeProperties.GetValueOrDefault("duration", 3),
                    (int)data.UpgradeProperties.GetValueOrDefault("maxStacks", 1)),
            ["Ability_Pe_Augment_Execute_Bosses"] = data =>
                new PeUpgradeExecuteBosses(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("stacksMultiplier", 2)),
            ["Ability_Pe_Augment_Spread_Poison"] = data =>
                new PeUpgradeSpreadPoison(
                    data.Id,
                    data.Tags,
                    data.Tier),
            ["Ability_Pe_Augment_Poison_Not_Removed"] = data =>
                new PeUpgradePreserveStacks(
                    data.Id,
                    data.Tags,
                    data.Tier),
            ["Augment_Reduce_Execution_Trahsold"] = data =>
                new PeUpgradeLowerExecutionThreshold(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    (int)data.UpgradeProperties.GetValueOrDefault("amount", 5)),
            ["Ability_Pe_Augment_Transfer_Poison_On_Death"] = data =>
                new PeUpgradeTransferPoisonOnDeath(
                    data.Id,
                    data.Tags,
                    data.Tier),
            ["Ability_Pe_Augment_Total_Damage_Multiplier"] = data =>
                new PeUpgradeTotalDamageMultiplier(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("multiplier", 0.15f)),
            ["Ability_Pc_Augment_Attacks_Reduce_Incoming_Heal"] = data =>
                new PcUpgradeApplyDebuffOnHit(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    () => new HealReductionEffect(
                        (int)data.UpgradeProperties.GetValueOrDefault("duration", 3),
                        (int)data.UpgradeProperties.GetValueOrDefault("maxStacks", 1),
                        data.UpgradeProperties.GetValueOrDefault("reduceBy", 0.6f))),
            ["Ability_Pc_Augment_Attacks_Reduce_Armor"] = data =>
                new PcUpgradeApplyDebuffOnHit(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    () => new ArmorReductionEffect(
                        (int)data.UpgradeProperties.GetValueOrDefault("duration", 3),
                        (int)data.UpgradeProperties.GetValueOrDefault("maxStacks", 4),
                        data.UpgradeProperties.GetValueOrDefault("reduceArmorBy", 0.15f))),
            ["Ability_Pc_Augment_Apply_Stack_For_Each_Enemy"] = data =>
                new PcUpgradeMultiStackOnHit(
                    data.Id,
                    data.Tags,
                    data.Tier),
            ["Ability_Pc_Augment_Increase_Poison_On_Target"] = data =>
                new PcUpgradeExtendExistingPoison(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    (int)data.UpgradeProperties.GetValueOrDefault("poisonDuration", 1)),
            ["Ability_Pc_Augment_Additional_Poison_Stack_Duration"] = data =>
                new PcUpgradeAdditionalPoisonDuration(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    (int)data.UpgradeProperties.GetValueOrDefault("additionalDuration", 1)),
            ["Ability_Pc_Augment_Additional_Multiplier"] = data =>
                new PcUpgradeAdditionalMultiplier(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("multiplier", 0.25f)),
            ["Ability_Pc_Augment_Increase_Duration"] = data =>
                new PcUpgradeIncreaseDuration(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    (int)data.UpgradeProperties.GetValueOrDefault("duration", 1)),
            ["Ability_Is_Augment_Additional_Scales"] = data =>
                new AbilityUpgradeAdditionalScales(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("weaponDamageScale", 0.05f),
                    data.UpgradeProperties.GetValueOrDefault("spellDamageScale", 0.15f)),
            ["Ability_Is_Augment_Additional_Crit_Damage"] = data =>
                new AbilityUpgradeAdditionalCritDamage(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("amount", 0.75f)),
            ["Ability_Is_Augment_Additional_Crit_Chance"] = data =>
                new AbilityUpgradeAdditionalCritChance(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("amount", 0.35f)),
            ["Ability_Is_Augment_Barrier_From_Shrapnel"] = data =>
                new IsUpgradeBarrierFromShrapnel(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("leachPercent", 0.15f)),
            ["Ability_Is_Augment_Apply_Fragility"] = data =>
                new IsUpgradeApplyFragility(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    (int)data.UpgradeProperties.GetValueOrDefault("duration", 3),
                    (int)data.UpgradeProperties.GetValueOrDefault("maxStacks", 3),
                    data.UpgradeProperties.GetValueOrDefault("critDamageAmp", 0.35f)),
            ["Ability_Is_Augment_Multicast"] = data =>
                new DelegateUpgrade<IceShards.IceShards>(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    ability => ability.Activation.BonusChance += data.UpgradeProperties.GetValueOrDefault("amount", 0.25f),
                    ability => ability.Activation.BonusChance -= data.UpgradeProperties.GetValueOrDefault("amount", 0.25f)),
            ["Ability_Is_Augment_Crit_Ignores_Cold_Res"] = data =>
                new DelegateUpgrade<IceShards.IceShards>(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    ability => ability.CritIgnoresColdResistance = true,
                    ability => ability.CritIgnoresColdResistance = false),
            ["Ability_Ia_Augment_Reflect"] = data =>
                new IaUpgradeParameter(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    IceAegis.IceAegis.Parameters.ReflectPercent,
                    data.UpgradeProperties.GetValueOrDefault("amount", 0.15f)),
            ["Ability_Ia_Augment_Stun_Attackers"] = data =>
                new IaUpgradeParameter(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    IceAegis.IceAegis.Parameters.StunAttackersChance,
                    data.UpgradeProperties.GetValueOrDefault("chance", 0.25f)),
            ["Ability_Ia_Augment_Turn_End_Heal"] = data =>
                new IaUpgradeParameter(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    IceAegis.IceAegis.Parameters.HealPerTurn,
                    data.UpgradeProperties.GetValueOrDefault("amount", 0.15f)),
            ["Ability_Ia_Augment_Crit_Mitigation"] = data =>
                new AbilityUpgradeCastEffect(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    ability => new EnhanceDefenseEffect(((IceAegis.IceAegis)ability).Duration, maxStacks: 1,
                        data.UpgradeProperties.GetValueOrDefault("amount", 0.8f))),
            ["Ability_Ib_Augment_Withering_Value"] = data =>
                new SimpleUpgrade<Ability>(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    new SimpleAbilityParameterDecorator(
                        IceBlocks.Parameters.WitheringValue, Priority.Weak, OperationType.Add,
                        data.UpgradeProperties.GetValueOrDefault("amount", 0.05f), $"Ability_Parameter_Decorator_{data.Id}", data.Id)),
            ["Ability_Ib_Augment_Withering_Stacks"] = data =>
                new SimpleUpgrade<Ability>(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    new SimpleAbilityParameterDecorator(
                        IceBlocks.Parameters.WitheringMaxStacks, Priority.Weak, OperationType.Add,
                        data.UpgradeProperties.GetValueOrDefault("amount", 1f), $"Ability_Parameter_Decorator_{data.Id}", data.Id)),
            ["Ability_Ib_Augment_Extra_Block_Damage"] = data =>
                new SimpleUpgrade<Ability>(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    new SimpleAbilityParameterDecorator(
                        IceBlocks.Parameters.ExtraBlockDamagePercent, Priority.Weak, OperationType.Add,
                        data.UpgradeProperties.GetValueOrDefault("amount", 0.25f), $"Ability_Parameter_Decorator_{data.Id}", data.Id)),
            ["Ability_Ib_Augment_Heavy_Blocks"] = data =>
                new AbilityUpgradeParameterSet(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    [
                        (AbilityParameter.Damage, data.UpgradeProperties.GetValueOrDefault("damage", 150f)),
                        (AbilityParameter.WeaponDamageScale, data.UpgradeProperties.GetValueOrDefault("weaponDamageScale", 0.15f)),
                        (AbilityParameter.SpellDamageScale, data.UpgradeProperties.GetValueOrDefault("spellDamageScale", 0.45f))
                    ]),
            ["Ability_Ib_Augment_Reset_Chance"] = data =>
                new DelegateUpgrade<IceBlocks>(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    ability => ability.ResetCooldownChance = data.UpgradeProperties.GetValueOrDefault("chance", 0.25f),
                    ability => ability.ResetCooldownChance = 0f),
            ["Ability_Ib_Augment_Random_Extra_Blocks"] = data =>
                new DelegateUpgrade<IceBlocks>(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    ability => ability.ExtraBlocksHitRandomTargets = true,
                    ability => ability.ExtraBlocksHitRandomTargets = false),
            ["Ability_Ib_Augment_Consume_Stun"] = data =>
                new DelegateUpgrade<IceBlocks>(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    ability => ability.ConsumeStunForDoubleDamage = true,
                    ability => ability.ConsumeStunForDoubleDamage = false),
            ["Ability_Df_Augment_Frostbite_Duration"] = data =>
                new SimpleUpgrade<Ability>(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    new SimpleAbilityParameterDecorator(
                        DeepFreeze.DeepFreeze.Parameters.FrostbiteDuration, Priority.Weak, OperationType.Add,
                        data.UpgradeProperties.GetValueOrDefault("amount", 1f), $"Ability_Parameter_Decorator_{data.Id}", data.Id)),
            ["Ability_Df_Augment_Spread_Freeze"] = data =>
                new DelegateUpgrade<DeepFreeze.DeepFreeze>(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    ability => ability.SpreadFreezeChance = data.UpgradeProperties.GetValueOrDefault("chance", 0.5f),
                    ability => ability.SpreadFreezeChance = 0f),
            ["Ability_Df_Augment_Extend_Effects"] = data =>
                new DelegateUpgrade<DeepFreeze.DeepFreeze>(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    ability => ability.ExtendTargetEffects = true,
                    ability => ability.ExtendTargetEffects = false),
            ["Ability_Df_Augment_More_Shred"] = data =>
                new SimpleUpgrade<Ability>(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    new SimpleAbilityParameterDecorator(
                        DeepFreeze.DeepFreeze.Parameters.ColdResistanceShred, Priority.Weak, OperationType.Add,
                        data.UpgradeProperties.GetValueOrDefault("amount", 0.15f), $"Ability_Parameter_Decorator_{data.Id}", data.Id)),
            ["Ability_Df_Augment_Enemy_Cooldown"] = data =>
                new AbilityUpgradeImpactRider(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    new ApplyEffectImpactRider(new NextAbilityCooldownEffect(
                        (int)data.UpgradeProperties.GetValueOrDefault("duration", 3),
                        data.UpgradeProperties.GetValueOrDefault("amount", 3f)))),
            ["Augment_Reduce_All_Cooldowns"] = data =>
                new AbilityUpgradeActivationRider(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    new ReduceAllCooldownsActivationRider(data.Id, (int)data.UpgradeProperties.GetValueOrDefault("amount", 2))),
            ["Ability_Df_Augment_Execute"] = data =>
                new AbilityUpgradeImpactRider(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    new ExecuteImpactRider(data.Id, data.UpgradeProperties.GetValueOrDefault("threshold", 0.30f))),
            ["Ability_Dis_Augment_Multiplier"] = data =>
                new SimpleUpgrade<Ability>(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    new SimpleAbilityParameterDecorator(
                        Discharge.Discharge.Parameters.BarrierMultiplier, Priority.Weak, OperationType.Add,
                        data.UpgradeProperties.GetValueOrDefault("amount", 0.5f), $"Ability_Parameter_Decorator_{data.Id}", data.Id)),
            ["Ability_Dis_Augment_More_Multiplier"] = data =>
                new SimpleUpgrade<Ability>(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    new SimpleAbilityParameterDecorator(
                        Discharge.Discharge.Parameters.BarrierMultiplier, Priority.Weak, OperationType.Add,
                        data.UpgradeProperties.GetValueOrDefault("amount", 1f), $"Ability_Parameter_Decorator_{data.Id}", data.Id)),
            ["Ability_Dis_Augment_More_Restore"] = data =>
                new SimpleUpgrade<Ability>(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    new SimpleAbilityParameterDecorator(
                        Discharge.Discharge.Parameters.StageThreeBarrierRestore, Priority.Weak, OperationType.Add,
                        data.UpgradeProperties.GetValueOrDefault("amount", 0.25f), $"Ability_Parameter_Decorator_{data.Id}", data.Id)),
            ["Ability_Dis_Augment_Spell_Scale"] = data =>
                new SimpleUpgrade<Ability>(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    new SimpleAbilityParameterDecorator(
                        AbilityParameter.SpellDamageScale, Priority.Weak, OperationType.Add,
                        data.UpgradeProperties.GetValueOrDefault("amount", 0.35f), $"Ability_Parameter_Decorator_{data.Id}", data.Id)),
            ["Ability_Dis_Augment_Overkill"] = data =>
                new DelegateUpgrade<Discharge.Discharge>(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    ability => ability.OverkillToRandom = true,
                    ability => ability.OverkillToRandom = false),
            ["Ability_Dis_Augment_Ignore_Resistances"] = data =>
                new DelegateUpgrade<Discharge.Discharge>(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    ability => ability.AlwaysIgnoreResistances = true,
                    ability => ability.AlwaysIgnoreResistances = false),
            ["Ability_Dis_Augment_Consume_Mana"] = data =>
                new DelegateUpgrade<Discharge.Discharge>(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    ability => ability.ConsumeManaInstead = true,
                    ability => ability.ConsumeManaInstead = false),
            ["Ability_Sa_Augment_Detonation_Scales"] = data =>
                new AbilityUpgradeParameterSet(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    [
                        (StaticArmor.StaticArmor.Parameters.DetonationWeaponScale, data.UpgradeProperties.GetValueOrDefault("weaponDamageScale", 0.25f)),
                        (StaticArmor.StaticArmor.Parameters.DetonationSpellScale, data.UpgradeProperties.GetValueOrDefault("spellDamageScale", 0.35f))
                    ]),
            ["Ability_Sa_Augment_Buff_Duration"] = data =>
                new SimpleUpgrade<Ability>(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    new SimpleAbilityParameterDecorator(
                        StaticArmor.StaticArmor.Parameters.Duration, Priority.Weak, OperationType.Add,
                        data.UpgradeProperties.GetValueOrDefault("amount", 1f), $"Ability_Parameter_Decorator_{data.Id}", data.Id)),
            ["Ability_Sa_Augment_More_Splash"] = data =>
                new SimpleUpgrade<Ability>(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    new SimpleAbilityParameterDecorator(
                        StaticArmor.StaticArmor.Parameters.StageThreeSplashDamage, Priority.Weak, OperationType.Add,
                        data.UpgradeProperties.GetValueOrDefault("amount", 0.5f), $"Ability_Parameter_Decorator_{data.Id}", data.Id)),
            ["Ability_Sa_Augment_Less_Stacks"] = data =>
                new SimpleUpgrade<Ability>(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    new SimpleAbilityParameterDecorator(
                        StaticArmor.StaticArmor.Parameters.RequiredStacks, Priority.Weak, OperationType.Subtract,
                        data.UpgradeProperties.GetValueOrDefault("amount", 1f), $"Ability_Parameter_Decorator_{data.Id}", data.Id)),
            ["Ability_Sa_Augment_Overkill"] = data =>
                new DelegateUpgrade<StaticArmor.StaticArmor>(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    ability => ability.OverkillToRandom = true,
                    ability => ability.OverkillToRandom = false),
            ["Ability_Sa_Augment_Ignore_Resistances"] = data =>
                new DelegateUpgrade<StaticArmor.StaticArmor>(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    ability => ability.IgnoreResistances = true,
                    ability => ability.IgnoreResistances = false),
            ["Augment_Cost_Barrier"] = data =>
                new AbilityUpgradeCostTypeOverride(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    Costs.Barrier),
        };
    }
}

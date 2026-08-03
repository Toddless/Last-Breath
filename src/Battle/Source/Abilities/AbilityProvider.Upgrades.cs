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
    using IncreasingPressure;
    using JarOfPoison;
    using PoisonCoating;
    using PoisonExplosion;
    using PassiveSkills;
    using Riders;
    using SeriesOfAttacks;
    using Core.Battle.Abilities;
    using Core.Modifiers.Context;
    using IceAegis;
    using IceBlock;
    using IceShards;

    public partial class AbilityProvider
    {
        /// <summary>
        /// The augments that need code — a behaviour to install, a strategy to swap, a member of one
        /// ability class to reach for, or a number measured as a share of the one it moves. An augment
        /// that only moves numbers is not written here: it is a row of the parameter table
        /// (<c>AbilityProvider.ParameterAugments.cs</c>) and shares its one class with all the others.
        /// The two halves never name the same augment.
        /// </summary>
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
            ["Ability_SoA_Augment_Attacks_Cannot_Be_Evaded"] = data =>
                new SoAsUpgradeUnevadable(
                    data.Id,
                    data.Tags,
                    data.Tier),
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
            ["Ability_Cl_Augment_Ignore_Resistances"] = data =>
                new DelegateUpgrade<ChainLightning.ChainLightning>(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    ability => ability.IgnoreResistances = true,
                    ability => ability.IgnoreResistances = false),
            ["Ability_Arm_Augment_Stage3_Burning"] = data =>
                new ArmUpgradeStage3Burning(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    (int)data.UpgradeProperties.GetValueOrDefault("stacks", 3),
                    (int)data.UpgradeProperties.GetValueOrDefault("duration", 3),
                    data.UpgradeProperties.GetValueOrDefault("damageMultiplier", 0.7f)),
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
            ["Ability_Porc_Augment_Armor_Buff"] = data =>
                new AbilityUpgradeCastEffect(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    ability => new ArmorBuffEffect(((Porcupine.Porcupine)ability).Duration, maxStacks: 1,
                        data.UpgradeProperties.GetValueOrDefault("amount", 0.25f))),
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
            ["Ability_Dst_Augment_Both_Hits_Buff"] = data =>
                new DstUpgradeBothHitsBuff(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("amount", 0.25f),
                    (int)data.UpgradeProperties.GetValueOrDefault("duration", 3)),
            ["Ability_Hb_Augment_Armor_Debuff"] = data =>
                new AbilityUpgradeImpactRider(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    new ApplyEffectImpactRider(new ArmorReductionEffect(
                        (int)data.UpgradeProperties.GetValueOrDefault("duration", 3),
                        (int)data.UpgradeProperties.GetValueOrDefault("maxStacks", 3),
                        data.UpgradeProperties.GetValueOrDefault("reduceArmorBy", 0.25f)))),
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

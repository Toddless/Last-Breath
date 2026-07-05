namespace Battle.Source.Abilities
{
    using System;
    using System.Collections.Generic;
    using Core.Data.AbilityData;
    using Core.Interfaces.Abilities;
    using CriticalCalculation;
    using DarkShroud;
    using Effects;
    using IceShrapnel;
    using IncreasingPressure;
    using JarOfPoison;
    using Modifiers;
    using PoisonCoating;
    using PoisonExplosion;
    using Riders;
    using SeriesOfAttacks;

    public partial class AbilityProvider
    {
        private readonly Dictionary<string, Func<AbilityUpgradeData, IAbilityUpgrade>> _abilityUpgrades = new()
        {
            ["Ability_SoA_Upgrade_Poison_On_Hit"] = data =>
                new SoAsUpgradePoisonOnHit(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    (int)data.UpgradeProperties.GetValueOrDefault("poisonDuration", 3)),
            ["Ability_SoA_Upgrade_Apply_Buff_Critical_Chance"] = data =>
                new SoAsUpgradeApplyBuffCriticalChance(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    (int)data.UpgradeProperties.GetValueOrDefault("amountAttacks", 6),
                    data.UpgradeProperties.GetValueOrDefault("criticalChance", 0.15f),
                    (int)data.UpgradeProperties.GetValueOrDefault("duration", 5),
                    (int)data.UpgradeProperties.GetValueOrDefault("maxStacks", 3f)),
            ["Ability_SoA_Upgrade_Apply_Buff_Critical_Damage"] = data =>
                new SoAsUpgradeApplyBuffCriticalDamage(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    (int)data.UpgradeProperties.GetValueOrDefault("amountAttacks", 9),
                    data.UpgradeProperties.GetValueOrDefault("criticalDamage", 0.25f),
                    (int)data.UpgradeProperties.GetValueOrDefault("duration", 5),
                    (int)data.UpgradeProperties.GetValueOrDefault("maxStacks", 3)),
            ["Ability_SoA_Upgrade_Additional_Max_Attacks"] = data =>
                new SoAsUpgradeMaxAmountAttacks(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    (int)data.UpgradeProperties.GetValueOrDefault("maxAdditionalAttacks", 3)),
            ["Ability_SoA_Upgrade_More_Attack_Damage"] = data =>
                new SoAsUpgradeMoreAttackDamage(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("damageMultiplier", 0.15f)),
            ["Ability_SoA_Upgrade_Attacks_Cannot_Be_Evaded"] = data =>
                new SoAsUpgradeUnevadable(
                    data.Id,
                    data.Tags,
                    data.Tier),
            ["Ability_SoA_Upgrade_Additional_Attacks"] = data =>
                new SoAsUpgradeAdditionalAttacks(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    (int)data.UpgradeProperties.GetValueOrDefault("amountAttacks", 1)),
            ["Ability_SoA_Upgrade_Reduce_Cost"] = data =>
                new AbilityUpgradeReduceCost(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("cost", 50)),
            ["Ability_SoA_Upgrade_Reduce_Cooldown"] = data =>
                new AbilityUpgradeReduceCooldown(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("cooldown", 1)),
            ["Ability_Ip_Upgrade_Single_Empowered_Attack"] = data =>
                new IpUpgradeSingleEmpoweredAttack(
                    data.Id,
                    data.Tags,
                    data.Tier),
            ["Ability_Ip_Upgrade_Attack_Random_Target"] = data =>
                new IpUpgradeAttackRandomTarget(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("splashDamage", 0.45f)),
            ["Ability_Ip_Upgrade_Last_Attack_Always_Crit"] = data =>
                new IpUpgradeLastAttackAlwaysCrit(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    new LastAttackAlwaysCritModifier()),
            ["Ability_Ip_Upgrade_First_Attack_Crit_Damage"] = data =>
                new IpUpgradeFirstAttackCritDamage(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    new FirstAttackCritModifier(data.UpgradeProperties.GetValueOrDefault("critDamageBonus", 1.3f))),
            ["Ability_Ip_Upgrade_Attack_Extend_Poison"] = data =>
                new IpUpgradeExtendPoison(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    (int)data.UpgradeProperties.GetValueOrDefault("poisonDuration", 1)),
            ["Ability_Ip_Upgrade_Unevadable"] = data =>
                new IpUpgradeUnevadable(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    new UnevadableAttackModifier()),
            ["Ability_Ip_Upgrade_Additional_Amount_Attacks"] = data =>
                new IpUpgradeAmountAttacks(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    (int)data.UpgradeProperties.GetValueOrDefault("amountAttacks", 2)),
            ["Ability_Ip_Upgrade_Additional_Damage_Multiplier"] = data =>
                new IpUpgradeAdditionalDamageMultiplier(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("damageMultiplier", 0.05f)),
            ["Ability_Ip_Upgrade_Reduce_Cooldown"] = data =>
                new AbilityUpgradeReduceCooldown(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    (int)data.UpgradeProperties.GetValueOrDefault("cooldown", 2)),
            ["Ability_JoP_Upgrade_Bouncing"] = data =>
                new JoPUpgradeBouncing(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    (int)data.UpgradeProperties.GetValueOrDefault("bounces", 5)),
            ["Ability_JoP_Upgrade_Transfer_Poison_On_Death"] = data =>
                new JoPUpgradeTransferOnDeath(
                    data.Id,
                    data.Tags,
                    data.Tier),
            ["Ability_JoP_Upgrade_All_Targets"] = data =>
                new JoPUpgradeAllTargets(
                    data.Id,
                    data.Tags,
                    data.Tier),
            ["Ability_JoP_Upgrade_Clumsiness"] = data =>
                new JoPDebuffUpgrade(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    new ApplyEffectImpactRider(
                        new Clumsiness(
                            (int)data.UpgradeProperties.GetValueOrDefault("duration", 5),
                            (int)data.UpgradeProperties.GetValueOrDefault("maxStacks", 5),
                            data.UpgradeProperties.GetValueOrDefault("evadeReduce", 0.05f)))),
            ["Ability_JoP_Upgrade_Apply_Blind"] = data =>
                new JoPDebuffUpgrade(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    new ApplyEffectImpactRider(new BlindEffect(
                        (int)data.UpgradeProperties.GetValueOrDefault("duration", 5),
                        (int)data.UpgradeProperties.GetValueOrDefault("maxStacks", 5),
                        data.UpgradeProperties.GetValueOrDefault("evadeReduce", 0.05f)))),
            ["Ability_JoP_Upgrade_Apply_Weakness"] = data =>
                new JoPDebuffUpgrade(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    new ApplyEffectImpactRider(new Weakness(
                        (int)data.UpgradeProperties.GetValueOrDefault("duration", 5),
                        (int)data.UpgradeProperties.GetValueOrDefault("maxStacks", 5),
                        data.UpgradeProperties.GetValueOrDefault("evadeReduce", 0.05f)))),
            ["Ability_JoP_Upgrade_Increasing_Scales"] = data =>
                new JoPUpgradeIncreasingScales(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("weaponDamageScale", 0.6f),
                    data.UpgradeProperties.GetValueOrDefault("spellDamageScale", 0.85f)),
            ["Ability_JoP_Upgrade_Poison_Duration"] = data =>
                new JoPUpgradePoisonDuration(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("poisonDuration", 1)),
            ["Ability_JoP_Upgrade_Reduce_Cooldown"] = data =>
                new AbilityUpgradeReduceCooldown(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("cooldown", 2)),
            ["Ability_Cc_Upgrade_Additional_Attack_Chance"] = data =>
                new CcUpgradeAdditionalAttackChance(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("value", 0.15f)),
            ["Ability_Cc_Upgrade_Additional_Crit_Chance"] = data =>
                new CcUpgradeIncreaseCritChance(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("criticalChance", 0.08f)),
            ["Ability_Cc_Upgrade_Lucky_Crit"] = data =>
                new CcUpgradeLuckyCrit(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    (int)data.UpgradeProperties.GetValueOrDefault("duration", 3)),
            ["Ability_Cc_Upgrade_Additional_Crit_Multiplier"] = data =>
                new CcUpgradeCritDamageBuff(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("multiplier", 0.15f),
                    data.UpgradeProperties.GetValueOrDefault("critChancePerHit", 0.15f),
                    (int)data.UpgradeProperties.GetValueOrDefault("duration", 3)),
            ["Ability_Cc_Upgrade_Apply_Enhanced_Defence"] = data =>
                new CcUpgradeApplyEnhancedDefence(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    (int)data.UpgradeProperties.GetValueOrDefault("duration", 3),
                    (int)data.UpgradeProperties.GetValueOrDefault("stacks", 3),
                    data.UpgradeProperties.GetValueOrDefault("value", 0.15f)),
            ["Ability_Cc_Upgrade_Leach_On_Crit"] = data =>
                new CcUpgradeLeachOnCrit(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("amount", 0.15f),
                    (int)data.UpgradeProperties.GetValueOrDefault("duration", 3)),
            ["Ability_Cc_Upgrade_More_Stacks_More_Cost"] = data =>
                new CcUpgradeMoreStacksMoreCost(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    (int)data.UpgradeProperties.GetValueOrDefault("stacks", 1),
                    data.UpgradeProperties.GetValueOrDefault("cost", 50)),
            ["Ability_Cc_Upgrade_Reduce_Cooldown"] = data =>
                new AbilityUpgradeReduceCooldown(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("cooldown", 1)),
            ["Ability_Cc_Upgrade_Reduce_Cost"] = data =>
                new AbilityUpgradeReduceCost(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("cost", 30)),
            ["Ability_Ds_Upgrade_Additional_Attack_Chance"] = data =>
                new DsUpgradeAdditionalAttackChance(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("amount", 0.15f)),
            ["Ability_Ds_Upgrade_Additional_Accuracy"] = data =>
                new DsUpgradeAdditionalAccuracy(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("amount", 0.2f)),
            ["Ability_Ds_Upgrade_Immortality"] = data =>
                new DsUpgradeImmortality(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("lifeToRecover", 0.35f),
                    (int)data.UpgradeProperties.GetValueOrDefault("duration", 3),
                    (int)data.UpgradeProperties.GetValueOrDefault("stacks", 1)),
            ["Ability_Ds_Upgrade_Mana_Regen"] = data =>
                new DsUpgradeManaRegen(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("regenAmount", 0.05f)),
            ["Ability_Ds_Upgrade_Additional_Health_Regen"] = data =>
                new DsUpgradeAdditionalHealthRegen(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("additionalRegen", 0.025f)),
            ["Ability_Ds_Upgrade_Add_Effectiveness_Reduce_Stacks"] = data =>
                new DsUpgradeAddEffectivenessReduceStacks(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("additionalEffectiveness", 0.35f),
                    (int)data.UpgradeProperties.GetValueOrDefault("amountStacks", 2)),
            ["Ability_Ds_Upgrade_Increased_Buff_Duration"] = data =>
                new DsUpgradeIncreasedBuffDuration(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("duration", 1)),
            ["Ability_Ds_Upgrade_Reduce_Cooldown"] = data =>
                new AbilityUpgradeReduceCooldown(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("cooldown", 1)),
            ["Ability_Ds_Upgrade_Reduce_Cost"] = data =>
                new AbilityUpgradeReduceCost(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("cost", 25)),
            ["Ability_Pe_Upgrade_Apply_Seal_Of_Oblivion"] = data =>
                new PeUpgradeApplySealOfOblivion(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    (int)data.UpgradeProperties.GetValueOrDefault("duration", 3),
                    (int)data.UpgradeProperties.GetValueOrDefault("maxStacks", 1)),
            ["Ability_Pe_Upgrade_Execute_Bosses"] = data =>
                new PeUpgradeExecuteBosses(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("stacksMultiplier", 2)),
            ["Ability_Pe_Upgrade_Spread_Poison"] = data =>
                new PeUpgradeSpreadPoison(
                    data.Id,
                    data.Tags,
                    data.Tier),
            ["Ability_Pe_Upgrade_Poison_Not_Removed"] = data =>
                new PeUpgradePreserveStacks(
                    data.Id,
                    data.Tags,
                    data.Tier),
            ["Ability_Pe_Upgrade_Reduce_Execution_Trahsold"] = data =>
                new PeUpgradeLowerExecutionThreshold(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    (int)data.UpgradeProperties.GetValueOrDefault("amount", 5)),
            ["Ability_Pe_Upgrade_Transfer_Poison_On_Death"] = data =>
                new PeUpgradeTransferPoisonOnDeath(
                    data.Id,
                    data.Tags,
                    data.Tier),
            ["Ability_Pe_Upgrade_Reduce_Cost"] = data =>
                new AbilityUpgradeReduceCost(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("cost", 30)),
            ["Ability_Pe_Upgrade_Reduce_Cooldown"] = data =>
                new AbilityUpgradeReduceCooldownAddCost(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("cooldown", 2),
                    data.UpgradeProperties.GetValueOrDefault("additionalCost", 50)),
            ["Ability_Pe_Upgrade_Total_Damage_Multiplier"] = data =>
                new PeUpgradeTotalDamageMultiplier(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("multiplier", 0.15f)),
            ["Ability_Pc_Upgrade_Attacks_Reduce_Incoming_Heal"] = data =>
                new PcUpgradeApplyDebuffOnHit(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    () => new HealReductionEffect(
                        (int)data.UpgradeProperties.GetValueOrDefault("duration", 3),
                        (int)data.UpgradeProperties.GetValueOrDefault("maxStacks", 1),
                        data.UpgradeProperties.GetValueOrDefault("reduceBy", 0.6f))),
            ["Ability_Pc_Upgrade_Attacks_Reduce_Armor"] = data =>
                new PcUpgradeApplyDebuffOnHit(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    () => new ArmorReductionEffect(
                        (int)data.UpgradeProperties.GetValueOrDefault("duration", 3),
                        (int)data.UpgradeProperties.GetValueOrDefault("maxStacks", 4),
                        data.UpgradeProperties.GetValueOrDefault("reduceArmorBy", 0.15f))),
            ["Ability_Pc_Upgrade_Apply_Stack_For_Each_Enemy"] = data =>
                new PcUpgradeMultiStackOnHit(
                    data.Id,
                    data.Tags,
                    data.Tier),
            ["Ability_Pc_Upgrade_Increase_Poison_On_Target"] = data =>
                new PcUpgradeExtendExistingPoison(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    (int)data.UpgradeProperties.GetValueOrDefault("poisonDuration", 1)),
            ["Ability_Pc_Upgrade_Additional_Poison_Stack_Duration"] = data =>
                new PcUpgradeAdditionalPoisonDuration(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    (int)data.UpgradeProperties.GetValueOrDefault("additionalDuration", 1)),
            ["Ability_Pc_Upgrade_Additional_Multiplier"] = data =>
                new PcUpgradeAdditionalMultiplier(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("multiplier", 0.25f)),
            ["Ability_Pc_Upgrade_Increase_Duration"] = data =>
                new PcUpgradeIncreaseDuration(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    (int)data.UpgradeProperties.GetValueOrDefault("duration", 1)),
            ["Ability_Pc_Upgrade_Reduce_Cooldown"] = data =>
                new AbilityUpgradeReduceCooldown(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("cooldown", 1)),
            ["Ability_Pc_Upgrade_Reduce_Cost"] = data =>
                new AbilityUpgradeReduceCost(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("cost", 30)),
            ["Ability_Is_Upgrade_Reduce_Cooldown_Add_Cost"] = data =>
                new AbilityUpgradeReduceCooldownAddCost(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("cooldown", 2),
                    data.UpgradeProperties.GetValueOrDefault("additionalCost", 100)),
            ["Ability_Is_Upgrade_Additional_Scales"] = data =>
                new AbilityUpgradeAdditionalScales(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("weaponDamageScale", 0.05f),
                    data.UpgradeProperties.GetValueOrDefault("spellDamageScale", 0.15f)),
            ["Ability_Is_Upgrade_Reduce_Cost"] = data =>
                new AbilityUpgradeReduceCost(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("cost", 70)),
            ["Ability_Is_Upgrade_Additional_Crit_Damage"] = data =>
                new AbilityUpgradeAdditionalCritDamage(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("amount", 0.75f)),
            ["Ability_Is_Upgrade_Additional_Crit_Chance"] = data =>
                new AbilityUpgradeAdditionalCritChance(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("amount", 0.35f)),
            ["Ability_Is_Upgrade_Barrier_From_Shrapnel"] = data =>
                new IsUpgradeBarrierFromShrapnel(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("leachPercent", 0.15f)),
            ["Ability_Is_Upgrade_Apply_Fragility"] = data =>
                new IsUpgradeApplyFragility(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    (int)data.UpgradeProperties.GetValueOrDefault("duration", 3),
                    (int)data.UpgradeProperties.GetValueOrDefault("maxStacks", 3),
                    data.UpgradeProperties.GetValueOrDefault("critDamageAmp", 0.35f)),
        };
    }
}

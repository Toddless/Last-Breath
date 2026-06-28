namespace Battle.Source.Abilities
{
    using System;
    using System.Collections.Generic;
    using Core.Data.AbilityData;
    using Core.Interfaces.Abilities;
    using Effects;
    using IncreasingPressure;
    using JarOfPoison;
    using SeriesOfAttacks;

    public partial class AbilityProvider
    {
        private readonly Dictionary<string, Func<AbilityUpgradeData, IAbilityUpgrade>> _abilityUpgrades = new()
        {
            ["Ability_Series_Of_Attacks_Upgrade_Poison_On_Hit"] = data =>
                new SoAsUpgradePoisonOnHit(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    (int)data.UpgradeProperties.GetValueOrDefault("poisonDuration", 3)),
            ["Ability_Series_Of_Attacks_Upgrade_Apply_Buff_Critical_Chance"] = data =>
                new SoAsUpgradeApplyBuffCriticalChance(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    (int)data.UpgradeProperties.GetValueOrDefault("amountAttacks", 6),
                    data.UpgradeProperties.GetValueOrDefault("criticalChance", 0.15f),
                    (int)data.UpgradeProperties.GetValueOrDefault("duration", 5),
                    (int)data.UpgradeProperties.GetValueOrDefault("maxStacks", 3f)),
            ["Ability_Series_Of_Attacks_Upgrade_Apply_Buff_Critical_Damage"] = data =>
                new SoAsUpgradeApplyBuffCriticalDamage(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    (int)data.UpgradeProperties.GetValueOrDefault("amountAttacks", 9),
                    data.UpgradeProperties.GetValueOrDefault("criticalDamage", 0.25f),
                    (int)data.UpgradeProperties.GetValueOrDefault("duration", 5),
                    (int)data.UpgradeProperties.GetValueOrDefault("maxStacks", 3)),
            ["Ability_Series_Of_Attacks_Upgrade_Additional_Max_Attacks"] = data =>
                new SoAsUpgradeMaxAmountAttacks(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    (int)data.UpgradeProperties.GetValueOrDefault("maxAdditionalAttacks", 3)),
            ["Ability_Series_Of_Attacks_Upgrade_More_Attack_Damage"] = data =>
                new SoAsUpgradeMoreAttackDamage(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("damageMultiplier", 0.15f)),
            ["Ability_Series_Of_Attacks_Upgrade_Attacks_Cannot_Be_Evaded"] = data =>
                new SoAsUpgradeUnevadable(
                    data.Id,
                    data.Tags,
                    data.Tier),
            ["Ability_Series_Of_Attacks_Upgrade_Additional_Attacks"] = data =>
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
            ["Ability_Increasing_Pressure_Upgrade_Single_Empowered_Attack"] = data =>
                new IpUpgradeSingleEmpoweredAttack(
                    data.Id,
                    data.Tags,
                    data.Tier),
            ["Ability_Increasing_Pressure_Upgrade_Attack_Random_Target"] = data =>
                new IpUpgradeAttackRandomTarget(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("splashDamage", 0.45f)),
            ["Ability_Increasing_Pressure_Upgrade_Last_Attack_Always_Crit"] = data =>
                new IpUpgradeLastAttackAlwaysCrit(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    new LastAttackAlwaysCritModifier()),
            ["Ability_Increasing_Pressure_Upgrade_First_Attack_Crit_Damage"] = data =>
                new IpUpgradeFirstAttackCritDamage(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    new FirstAttackCritModifier(data.UpgradeProperties.GetValueOrDefault("critDamageBonus", 1.3f))),
            ["Ability_Increasing_Pressure_Upgrade_Attack_Extend_Poison"] = data =>
                new IpUpgradeExtendPoison(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    (int)data.UpgradeProperties.GetValueOrDefault("poisonDuration", 1)),
            ["Ability_Increasing_Pressure_Upgrade_Unevadable"] = data =>
                new IpUpgradeUnevadable(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    new UnevadableAttackModifier()),
            ["Ability_Increasing_Pressure_Upgrade_Additional_Amount_Attacks"] = data =>
                new IpUpgradeAmountAttacks(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    (int)data.UpgradeProperties.GetValueOrDefault("amountAttacks", 2)),
            ["Ability_Increasing_Pressure_Upgrade_Additional_Damage_Multiplier"] = data =>
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
                    new ApplyDebuffPostActivationModifier(
                        new Clumsiness(
                            (int)data.UpgradeProperties.GetValueOrDefault("duration", 5),
                            (int)data.UpgradeProperties.GetValueOrDefault("maxStacks", 5),
                            data.UpgradeProperties.GetValueOrDefault("evadeReduce", 5)))),
            ["Ability_JoP_Upgrade_Apply_Blind"] = data =>
                new JoPDebuffUpgrade(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    new ApplyDebuffPostActivationModifier(new BlindEffect(
                        (int)data.UpgradeProperties.GetValueOrDefault("duration", 5),
                        (int)data.UpgradeProperties.GetValueOrDefault("maxStacks", 5),
                        data.UpgradeProperties.GetValueOrDefault("evadeReduce", 5)))),
            ["Ability_JoP_Upgrade_Apply_Weakness"] = data =>
                new JoPDebuffUpgrade(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    new ApplyDebuffPostActivationModifier(new Weakness(
                        (int)data.UpgradeProperties.GetValueOrDefault("duration", 5),
                        (int)data.UpgradeProperties.GetValueOrDefault("maxStacks", 5),
                        data.UpgradeProperties.GetValueOrDefault("evadeReduce", 5)))),
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
        };
    }
}

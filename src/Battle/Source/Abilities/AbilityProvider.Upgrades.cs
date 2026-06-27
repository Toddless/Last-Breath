namespace Battle.Source.Abilities
{
    using System;
    using SeriesOfAttacks;
    using Core.Data.AbilityData;
    using Core.Interfaces.Abilities;
    using System.Collections.Generic;

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
            ["Ability_Series_Of_Attacks_Upgrade_Reduce_Ability_Cost"] = data =>
                new AbilityUpgradeReduceCost(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("cost", 50)),
            ["Ability_Series_Of_Attacks_Upgrade_Reduce_Ability_Cooldown"] = data =>
                new AbilityUpgradeReduceCooldown(
                    data.Id,
                    data.Tags,
                    data.Tier,
                    data.UpgradeProperties.GetValueOrDefault("cooldown", 1))
        };
    }
}

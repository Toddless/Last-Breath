namespace Battle.Source.Abilities
{
    using System;
    using System.Collections.Generic;
    using Core.Data.AbilityData;
    using Core.Interfaces.Abilities;
    using Targeting;

    public partial class AbilityProvider
    {
        private readonly Dictionary<string, Func<AbilityBaseData, IAbility>> _abilityFactories = new()
        {
            ["Ability_Ice_Shards"] = data => new IceShards.IceShards(
                data.Tags,
                data.Cooldown,
                data.CostValue,
                data.Damage,
                data.WeaponDamageScale,
                data.SpellDamageScale,
                (int)data.AbilityProperties.GetValueOrDefault("projectiles", 3),
                data.AbilityProperties.GetValueOrDefault("shrapnelDamage", 50f),
                data.AbilityProperties.GetValueOrDefault("shrapnelWeaponDamageScale", 0.15f),
                data.AbilityProperties.GetValueOrDefault("shrapnelSpellDamageScale", 0.55f),
                data.AbilityProperties.GetValueOrDefault("secondStageDamage", 120f),
                data.AbilityProperties.GetValueOrDefault("secondStageWeaponDamageScale", 0.35f),
                data.AbilityProperties.GetValueOrDefault("secondStageSpellDamageScale", 1.2f),
                data.CostsType) { Stance = data.Stance, MasteryLevel = data.MasteryLevel, Targeting = TargetingStrategyFactory.From(data), },
            ["Ability_Series_Of_Attacks"] = data => new SeriesOfAttacks.SeriesOfAttacks(
                data.Tags,
                data.Cooldown,
                data.CostValue,
                data.Damage,
                data.WeaponDamageScale,
                data.SpellDamageScale,
                (int)data.AbilityProperties.GetValueOrDefault("minAttacks", 2),
                (int)data.AbilityProperties.GetValueOrDefault("maxAttacks", 5),
                data.AbilityProperties.GetValueOrDefault("damageMultiplier", 1.3f),
                data.CostsType) { Stance = data.Stance, MasteryLevel = data.MasteryLevel, Targeting = TargetingStrategyFactory.From(data), },
            ["Ability_Poison_Explosion"] = data => new PoisonExplosion.PoisonExplosion(
                data.Tags,
                data.Cooldown,
                data.CostValue,
                (int)data.AbilityProperties.GetValueOrDefault("executionThreshold", 42),
                data.AbilityProperties.GetValueOrDefault("multiplier", 0f))
            {
                Stance = data.Stance, MasteryLevel = data.MasteryLevel, Targeting = TargetingStrategyFactory.From(data),
            },
            ["Ability_Poison_Coating"] = data => new PoisonCoating.PoisonCoating(
                data.Tags,
                data.Cooldown,
                data.CostValue,
                (int)data.AbilityProperties.GetValueOrDefault("buffDuration", 3),
                (int)data.AbilityProperties.GetValueOrDefault("poisonDuration", 5),
                data.AbilityProperties.GetValueOrDefault("poisonMultiplier", 0.45f),
                data.CostsType) { Stance = data.Stance, MasteryLevel = data.MasteryLevel, Targeting = TargetingStrategyFactory.From(data), },
            ["Ability_Jar_Of_Poison"] = data => new JarOfPoison.JarOfPoison(
                data.Tags,
                data.Cooldown,
                data.CostValue,
                data.Damage,
                data.WeaponDamageScale,
                data.SpellDamageScale,
                3,
                data.CostsType) { Stance = data.Stance, MasteryLevel = data.MasteryLevel, Targeting = TargetingStrategyFactory.From(data), },
            ["Ability_Increasing_Pressure"] = data => new IncreasingPressure.IncreasingPressure(
                data.Tags,
                data.CostValue,
                data.Cooldown,
                data.Damage,
                data.WeaponDamageScale,
                data.SpellDamageScale,
                (int)data.AbilityProperties.GetValueOrDefault("attacks", 5),
                data.AbilityProperties.GetValueOrDefault("damageMultiplier", 0.15f),
                data.CostsType) { Stance = data.Stance, MasteryLevel = data.MasteryLevel, Targeting = TargetingStrategyFactory.From(data), },
            ["Ability_Dark_Shroud"] = data => new DarkShroud.DarkShroud(
                data.Tags,
                data.Cooldown,
                data.CostValue,
                (int)data.AbilityProperties.GetValueOrDefault("stacks", 3),
                data.AbilityProperties.GetValueOrDefault("healthRegen", 0.05f),
                data.AbilityProperties.GetValueOrDefault("lightStepValue", 0.15f),
                (int)data.AbilityProperties.GetValueOrDefault("duration", 3),
                data.AbilityProperties.GetValueOrDefault("effectiveness", 1f),
                data.CostsType) { Stance = data.Stance, MasteryLevel = data.MasteryLevel, Targeting = TargetingStrategyFactory.From(data), },
            ["Ability_Critical_Calculation"] = data => new CriticalCalculation.CriticalCalculation(
                data.Tags,
                data.Cooldown,
                data.CostValue,
                (int)data.AbilityProperties.GetValueOrDefault("buffStacks", 3),
                (int)data.AbilityProperties.GetValueOrDefault("additionalBuffDuration", 1),
                data.CostsType) { Stance = data.Stance, MasteryLevel = data.MasteryLevel, Targeting = TargetingStrategyFactory.From(data), }
        };
    }
}

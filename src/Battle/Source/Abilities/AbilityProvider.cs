namespace Battle.Source.Abilities
{
    using System;
    using Core.Data.AbilityData;
    using System.Collections.Generic;
    using Core.Interfaces.Abilities;

    public class AbilityProvider
    {
        private const string Path = "res://Data/";
        private readonly Dictionary<string, AbilityBaseData> _abilityBaseData = [];

        private readonly Dictionary<string, Func<AbilityBaseData, IAbility>> _abilityFactories = new()
        {
            ["Ability_Series_Of_Attacks"] = data => new SeriesOfAttacks.SeriesOfAttacks(data.Tags, data.Cooldown, data.CostValue, data.Damage, data.WeaponDamageScale,
                data.SpellDamageScale, data.Upgrades, 1, 5, 1.3f, data.CostsType),
            ["Ability_Poison_Explosion"] = data => new PoisonExplosion.PoisonExplosion(data.Tags, data.Cooldown, data.CostValue, data.Upgrades, 42, 1.15f),
            ["Ability_Poison_Coating"] = data => new PoisonCoating.PoisonCoating(data.Tags, data.Cooldown, data.CostValue, data.Upgrades, 3, 3, 0.7f, data.CostsType),
            ["Ability_Jar_Of_Poison"] =
                data => new JarOfPoison.JarOfPoison(data.Tags, data.Cooldown, data.CostValue, data.Damage, data.WeaponDamageScale, data.SpellDamageScale, data.Upgrades, 3,
                    data.CostsType),
            ["Ability_Increasing_Pressure"] = data => new IncreasingPressure.IncreasingPressure(data.Tags, data.Cooldown, data.CostValue, data.Damage, data.WeaponDamageScale,
                data.SpellDamageScale, data.Upgrades, 7, 0.15f, data.CostsType),
            ["Ability_Dark_Shroud"] =
                data => new DarkShroud.DarkShroud(data.Tags, data.Cooldown, data.CostValue, data.Upgrades, buffDuration: 3, buffEffectiveness: 1f, data.CostsType),
            ["Ability_Critical_Calculation"] = data => new CriticalCalculation.CriticalCalculation(data.Tags, data.Cooldown, data.CostValue, data.Upgrades, 3, 3, data.CostsType)
        };


        public IAbility CreateAbility(string abilityId)
        {
            _abilityFactories.TryGetValue(abilityId, out var ability);
            _abilityBaseData.TryGetValue(abilityId, out var abilityBaseData);
            ArgumentNullException.ThrowIfNull(abilityBaseData);
            ArgumentNullException.ThrowIfNull(ability);

            return ability(abilityBaseData);
        }
    }
}

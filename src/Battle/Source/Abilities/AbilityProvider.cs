namespace Battle.Source.Abilities
{
    using System;
    using Core.Data.AbilityData;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.Interfaces.Abilities;
    using SeriesOfAttacks;

    public partial class AbilityProvider
    {
        private const string Path = "res://Data/";
        private readonly Dictionary<string, AbilityBaseData> _abilityBaseData = [];

        public IAbility CreateAbility(string abilityId)
        {
            _abilityFactories.TryGetValue(abilityId, out var ability);
            _abilityBaseData.TryGetValue(abilityId, out var abilityBaseData);
            ArgumentNullException.ThrowIfNull(abilityBaseData);
            ArgumentNullException.ThrowIfNull(ability);
            var newAbility = ability(abilityBaseData);
            newAbility.SetAbilityUpgrades(CreateAbilityUpgrades(abilityBaseData.Upgrades));
            return newAbility;
        }

        private Dictionary<int, List<IAbilityUpgrade>> CreateAbilityUpgrades(Dictionary<int, List<AbilityUpgradeData>> data)
        {
            return [];
        }


        private  Task LoadDataAsync()
        {
            return Task.CompletedTask;
        }
    }
}

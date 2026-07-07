namespace Battle.Source.Abilities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core;
    using Core.Battle.Abilities;
    using Core.Data;
    using Core.Data.AbilityData;
    using Core.Enums;
    using Newtonsoft.Json;

    public partial class AbilityProvider : IAbilityProvider
    {
        private const string DataPath = "res://Data/";
        private readonly Dictionary<string, AbilityBaseData> _abilityBaseData = [];

        public AbilityProvider() => _ = LoadDataAsync();

        public IReadOnlyCollection<string> KnownAbilityIds => _abilityBaseData.Keys;

        public IAbility CreateAbility(string abilityId)
        {
            var data = GetBaseData(abilityId);
            var ability = GetFactory(abilityId).Invoke(data);
            ability.SetAbilityUpgrades(CopyAbilityUpgrades(data.Upgrades));
            return ability;
        }

        /// <summary>The stance an ability belongs to — the ability book partitions by it on Learn.</summary>
        public Stance GetAbilityStance(string abilityId) => GetBaseData(abilityId).Stance;

        /// <summary>The mastery level at which the ability becomes learnable.</summary>
        public int GetMasteryLevel(string abilityId) => GetBaseData(abilityId).MasteryLevel;

        private AbilityBaseData GetBaseData(string abilityId) =>
            _abilityBaseData.GetValueOrDefault(abilityId)
            ?? throw new KeyNotFoundException($"No base data loaded for ability '{abilityId}'");

        private Func<AbilityBaseData, IAbility> GetFactory(string abilityId) =>
            _abilityFactories.GetValueOrDefault(abilityId)
            ?? throw new KeyNotFoundException($"No factory registered for ability '{abilityId}'");

        private Dictionary<int, List<IAbilityUpgrade>> CopyAbilityUpgrades(List<AbilityUpgradeData> data) =>
            data.GroupBy(upgrade => upgrade.Tier)
                .ToDictionary(tier => tier.Key, tier => tier.Select(CreateUpgrade).OfType<IAbilityUpgrade>().ToList());

        private IAbilityUpgrade? CreateUpgrade(AbilityUpgradeData data)
        {
            if (_abilityUpgrades.TryGetValue(data.Id, out var factory)) return factory(data);

            Tracker.TrackNotFound($"Upgrade factory '{data.Id}'", this);
            return null;
        }

        private async Task LoadDataAsync()
        {
            try
            {
                await DataLoader.LoadDataFromJson(DataPath, ParseAbilities);
            }
            catch (Exception e)
            {
                Tracker.TrackException("Failed to load ability data", e);
            }
        }

        private Task ParseAbilities(string json)
        {
            var root = JsonConvert.DeserializeObject<AbilityDataRoot>(json)
                       ?? throw new InvalidOperationException("Failed to deserialize ability data");
            foreach (AbilityBaseData abilityData in root.Abilities)
                _abilityBaseData[abilityData.Id] = abilityData;
            return Task.CompletedTask;
        }
    }
}

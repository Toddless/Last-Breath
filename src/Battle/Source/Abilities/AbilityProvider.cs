namespace Battle.Source.Abilities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core;
    using Core.Battle.Abilities;
    using Core.Data.AbilityData;
    using Core.Data.GameData;
    using Core.Enums;
    using Newtonsoft.Json;

    public partial class AbilityProvider : IAbilityProvider, IGameDataParticipant
    {
        private readonly Dictionary<string, AbilityBaseData> _abilityBaseData = [];

        public IReadOnlyList<string> Catalogs => [DataCatalog.Abilities];

        public IReadOnlyCollection<string> KnownAbilityIds => _abilityBaseData.Keys;

        public void Apply(string catalog, GameDataFile file)
        {
            var root = JsonConvert.DeserializeObject<AbilityDataRoot>(file.Json)
                       ?? throw new InvalidOperationException("Failed to deserialize ability data");
            foreach (AbilityBaseData abilityData in root.Abilities)
                _abilityBaseData[abilityData.Id] = abilityData;
        }

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

        /// <summary>Internal-cast-only ability (boss reactions): never learnable, never shown in trees.</summary>
        public bool IsHidden(string abilityId) => GetBaseData(abilityId).Hidden;

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
            if (_abilityUpgrades.TryGetValue(data.Id, out var factory))
            {
                var upgrade = factory(data);
                // Placeholder = json property name; live upgrade descriptions come for free
                upgrade.DescriptionValues = data.UpgradeProperties.ToDictionary(entry => entry.Key, object? (entry) => entry.Value);
                return upgrade;
            }

            Tracker.TrackNotFound($"Upgrade factory '{data.Id}'", this);
            return null;
        }
    }
}

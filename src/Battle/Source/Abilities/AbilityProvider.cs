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

    /// <summary>
    /// The abilities as their data declares them, and — through <see cref="IAbilityAugmentCatalog"/> —
    /// what the ids of an augment install mean. Both answers come out of the same parsed records, so
    /// the tags a fit is judged by are the tags the ability is built from.
    /// </summary>
    public partial class AbilityProvider : IAbilityProvider, IAbilityAugmentCatalog, IGameDataParticipant
    {
        private readonly Dictionary<string, AbilityBaseData> _abilityBaseData = [];

        /// <summary>Every augment record by its own id. The records are declared inside the abilities
        /// and asked for by id alone: the fit of an augment nobody owns yet is a question about a
        /// record, and the asker knows the id it was offered, not where it is written.</summary>
        private readonly Dictionary<string, AbilityUpgradeData> _augments = [];

        public IReadOnlyList<string> Catalogs => [DataCatalog.Abilities];

        public IReadOnlyCollection<string> KnownAbilityIds => _abilityBaseData.Keys;

        public void Apply(string catalog, GameDataFile file)
        {
            var root = JsonConvert.DeserializeObject<AbilityDataRoot>(file.Json)
                       ?? throw new InvalidOperationException("Failed to deserialize ability data");
            foreach (AbilityBaseData abilityData in root.Abilities)
            {
                _abilityBaseData[abilityData.Id] = abilityData;
                foreach (AbilityUpgradeData augment in abilityData.Upgrades) _augments[augment.Id] = augment;
            }
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

        /// <summary>Internal-cast-only ability (boss reactions): never learnable, never shown in trees.</summary>
        public bool IsHidden(string abilityId) => GetBaseData(abilityId).Hidden;

        /// <summary>The augment's record exactly as its data declares it. An id no file declares is
        /// answered with null rather than an empty record: nothing says what such an augment is, and
        /// a blank record would read as one declaring nothing — which is a record the rule can judge.</summary>
        public AbilityUpgradeData? Find(string augmentId) => _augments.GetValueOrDefault(augmentId);

        /// <summary>The combat tags of an ability, empty for an id the data does not declare. Unknown
        /// is not an error here: the catalog answers about ids that arrive from a save file or a drop,
        /// and an ability that left the game takes its tags with it.</summary>
        public IReadOnlyCollection<string> TagsOf(string abilityId) =>
            _abilityBaseData.TryGetValue(abilityId, out AbilityBaseData? ability) ? ability.Tags : [];

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

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

        /// <summary>Every augment record by its own id, out of the section that declares them. They
        /// are asked for by id alone: the fit of an augment nobody owns yet is a question about a
        /// record, and the asker knows the id it was offered, not where it is written.</summary>
        private readonly Dictionary<string, AbilityUpgradeData> _augments = [];

        public IReadOnlyList<string> Catalogs => [DataCatalog.Abilities];

        public IReadOnlyCollection<string> KnownAbilityIds => _abilityBaseData.Keys;

        /// <summary>Every augment the data declares, whatever it ends up fitting.</summary>
        public IReadOnlyCollection<string> KnownAugmentIds => _augments.Keys;

        /// <summary>Every augment this build knows how to make an upgrade of. A record the data
        /// declares and this collection does not name is a record that parses, is offered and then
        /// silently produces nothing.</summary>
        public IReadOnlyCollection<string> BuildableAugmentIds => _abilityUpgrades.Keys;

        public void Apply(string catalog, GameDataFile file)
        {
            var root = JsonConvert.DeserializeObject<AbilityDataRoot>(file.Json)
                       ?? throw new InvalidOperationException("Failed to deserialize ability data");
            foreach (AbilityBaseData abilityData in root.Abilities) _abilityBaseData[abilityData.Id] = abilityData;

            foreach (AbilityUpgradeData augment in root.Augments) _augments[augment.Id] = augment;
        }

        public IAbility CreateAbility(string abilityId)
        {
            var data = GetBaseData(abilityId);
            var ability = GetFactory(abilityId).Invoke(data);
            ability.SetAbilityUpgrades(AugmentsOffered(data));
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

        /// <summary>The augments an ability may wear, grouped by the tier each of them is written at.
        /// The offering is no longer a list the ability declares: every record of the catalog is put
        /// to the fitting rule against a slot of this ability, so what answers is the augment's own
        /// declaration. Nothing is passed as already worn — the offering says what may ever go in,
        /// while an exclusion group speaks about what is sitting there right now.</summary>
        private Dictionary<int, List<IAbilityUpgrade>> AugmentsOffered(AbilityBaseData ability) =>
            _augments.Values
                .Where(augment => BelongsOn(ability, augment))
                .GroupBy(augment => augment.Tier)
                .ToDictionary(group => group.Key, group => group.Select(CreateUpgrade).OfType<IAbilityUpgrade>().ToList());

        /// <summary>Whether the augment belongs on that ability at all, asked of the one rule that
        /// answers it. The slot handed over is the ability's own, opened exactly deep enough for the
        /// record — the offering is about belonging, and a socket too shallow is a question for the
        /// board that holds the real slots. It carries no id because no slot is meant: nothing here
        /// is being seated.</summary>
        private static bool BelongsOn(AbilityBaseData ability, AbilityUpgradeData augment) =>
            AugmentFit.Check(
                new AbilitySocketPlacement(string.Empty, ability.Id, augment.Tier),
                ability.Tags,
                augment,
                []) == AugmentFitResult.Fits;

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

namespace Core.Battle.Abilities
{
    using System;
    using System.Collections.Generic;
    using Data.AbilityData;
    using Data.GameData;
    using Newtonsoft.Json;

    /// <summary>
    /// The augment records as their own section declares them, read out of the ability catalog. The
    /// section is written beside the abilities, but what an augment id means is not a battle question:
    /// the same id has to be answerable wherever augments are offered, judged or handled, and those
    /// places are compositions that build no battle module at all. Hence the reading lives here, in
    /// the assembly every composition holds — the battle module builds the augments, this says what
    /// they are.
    /// <para>
    /// The tags of the abilities are taken off the same files, because half a catalog cannot answer:
    /// the fitting rule measures an augment's declaration against the tags of the ability whose slot
    /// is asked about, and a reader able to produce only the record would leave every such question to
    /// a module it cannot see.
    /// </para>
    /// </summary>
    public class AbilityAugmentCatalog : IAbilityAugmentCatalog, IGameDataParticipant
    {
        /// <summary>Every augment record by its own id. They are asked for by id alone: the fit of an
        /// augment nobody owns yet is a question about a record, and the asker knows the id it was
        /// offered, not where it is written.</summary>
        private readonly Dictionary<string, AbilityUpgradeData> _augments = new(StringComparer.Ordinal);

        private readonly Dictionary<string, string[]> _abilityTags = new(StringComparer.Ordinal);

        public IReadOnlyList<string> Catalogs => [DataCatalog.Abilities];

        public IReadOnlyCollection<AbilityUpgradeData> All => _augments.Values;

        public void Apply(string catalog, GameDataFile file)
        {
            var root = JsonConvert.DeserializeObject<AbilityDataRoot>(file.Json)
                       ?? throw new InvalidOperationException($"Failed to deserialize ability data '{file.FileName}'");

            foreach (AbilityUpgradeData augment in root.Augments) _augments[augment.Id] = augment;

            foreach (AbilityBaseData ability in root.Abilities) _abilityTags[ability.Id] = ability.Tags;
        }

        /// <summary>The augment's record exactly as its data declares it. An id no file declares is
        /// answered with null rather than an empty record: nothing says what such an augment is, and
        /// a blank record would read as one declaring nothing — which is a record the rule can judge.</summary>
        public AbilityUpgradeData? Find(string augmentId) => _augments.GetValueOrDefault(augmentId);

        /// <summary>The combat tags of an ability, empty for an id the data does not declare. Unknown
        /// is not an error here: the catalog answers about ids that arrive from a save file or a drop,
        /// and an ability that left the game takes its tags with it.</summary>
        public IReadOnlyCollection<string> TagsOf(string abilityId) => _abilityTags.GetValueOrDefault(abilityId, []);
    }
}

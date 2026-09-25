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
    /// The abilities of the same files are read whole rather than for their tags alone. Half a catalog
    /// cannot answer: the fitting rule measures an augment's declaration against the tags of the
    /// ability whose slot is asked about, and what an ability is — its stance, its cost, whether it is
    /// castable by a player at all — is asked by editors and listings that build no battle module. The
    /// records are handed out as they were parsed; they are immutable, and nothing here copies them.
    /// </para>
    /// </summary>
    public class AbilityAugmentCatalog : IAbilityAugmentCatalog, IGameDataParticipant
    {
        /// <summary>What a duplicate declaration is called when it is reported.</summary>
        private const string AugmentRecord = "Augment";
        private const string AbilityRecord = "Ability";

        /// <summary>Every augment record by its own id. They are asked for by id alone: the fit of an
        /// augment nobody owns yet is a question about a record, and the asker knows the id it was
        /// offered, not where it is written.</summary>
        private readonly Dictionary<string, AbilityAugmentData> _augments = new(StringComparer.Ordinal);

        /// <summary>Every ability record by its own id, hidden ones included — the id is what a save
        /// entry, a passive node and a spawn table all name an ability by.</summary>
        private readonly Dictionary<string, AbilityBaseData> _abilities = new(StringComparer.Ordinal);

        public IReadOnlyList<string> Catalogs => [DataCatalog.Abilities];

        public IReadOnlyCollection<AbilityAugmentData> All => _augments.Values;

        public IReadOnlyCollection<AbilityBaseData> Abilities => _abilities.Values;

        public IReadOnlyCollection<string> AbilityIds => _abilities.Keys;

        public void Apply(string catalog, GameDataFile file)
        {
            var root = JsonConvert.DeserializeObject<AbilityDataRoot>(file.Json)
                       ?? throw new InvalidOperationException($"Failed to deserialize ability data '{file.FileName}'");

            foreach (AbilityAugmentData augment in root.Augments) Declare(_augments, augment.Id, augment, AugmentRecord, file);

            foreach (AbilityBaseData ability in root.Abilities) Declare(_abilities, ability.Id, ability, AbilityRecord, file);
        }

        /// <summary>Takes a record in, last declaration winning. A second record under the same id is
        /// named out loud: the reader has no way of choosing between them, and an author whose two
        /// entries silently collapse into one is left wondering which of his numbers the game runs.</summary>
        private static void Declare<TRecord>(
            Dictionary<string, TRecord> records, string id, TRecord record, string kind, GameDataFile file)
        {
            if (records.ContainsKey(id))
                Tracker.TrackError($"{kind} '{id}' is declared more than once ('{file.FileName}'); the last declaration wins");

            records[id] = record;
        }

        /// <summary>The augment's record exactly as its data declares it. An id no file declares is
        /// answered with null rather than an empty record: nothing says what such an augment is, and
        /// a blank record would read as one declaring nothing — which is a record the rule can judge.</summary>
        public AbilityAugmentData? Find(string augmentId) => _augments.GetValueOrDefault(augmentId);

        /// <summary>The ability's record exactly as its data declares it, hidden abilities included —
        /// what is castable is a question about the record, not about which records are handed out.
        /// Null for an id no file declares, for the reason the augment lookup is.</summary>
        public AbilityBaseData? FindAbility(string abilityId) => _abilities.GetValueOrDefault(abilityId);

        /// <summary>The combat tags of an ability, empty for an id the data does not declare. Unknown
        /// is not an error here: the catalog answers about ids that arrive from a save file or a drop,
        /// and an ability that left the game takes its tags with it.</summary>
        public IReadOnlyCollection<string> TagsOf(string abilityId) => FindAbility(abilityId)?.Tags ?? [];
    }
}

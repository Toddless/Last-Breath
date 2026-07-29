namespace PassiveTreeEditor.Source.Io
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Data.AbilityData;
    using Core.Data.GameData;
    using Core.Enums;
    using Newtonsoft.Json;

    /// <summary>
    /// The ability list, read from the game's own <c>Abilities</c> catalog through the game's own
    /// participant contract — nothing about abilities is hard-coded in the tool, so a new ability in
    /// BaseAbilityData.json shows up in the node pickers with no code change.
    /// </summary>
    public sealed class AbilityCatalog : IGameDataParticipant
    {
        private readonly List<AbilityEntry> _abilities = [];

        private IReadOnlyList<AbilityEntry>? _selectable;
        private bool _sorted;

        public IReadOnlyList<string> Catalogs => [DataCatalog.Abilities];

        /// <summary>Every ability of the catalog, stance first then id, for stable picker order.</summary>
        public IReadOnlyList<AbilityEntry> Abilities
        {
            get
            {
                Sort();
                return _abilities;
            }
        }

        /// <summary>Sorting is deferred to the first read because <see cref="Apply"/> runs once per
        /// file: sorting there would re-sort the whole catalog for every file it grows by.</summary>
        private void Sort()
        {
            if (_sorted) return;

            _abilities.Sort(static (first, second) =>
            {
                int byStance = first.Stance.CompareTo(second.Stance);
                return byStance != 0 ? byStance : string.CompareOrdinal(first.Id, second.Id);
            });

            _sorted = true;
        }

        public void Apply(string catalog, GameDataFile file)
        {
            AbilityDataRoot root = JsonConvert.DeserializeObject<AbilityDataRoot>(file.Json)
                                   ?? throw new InvalidOperationException($"Failed to deserialize ability data from {file.FileName}");

            // Entries accumulate instead of replacing: a catalog is a folder of files, and the
            // participant contract has no "a fresh load starts here" hook to clear on.
            foreach (AbilityBaseData ability in root.Abilities)
                _abilities.Add(new AbilityEntry(ability.Id, ability.Stance, ability.Hidden));

            _sorted = false;
            _selectable = null;
        }

        /// <summary>Abilities a tree node may reference. Hidden ones are internal-cast only and, by
        /// their own data comment, never appear in trees. Cached: the inspector asks on every rebuild,
        /// and the answer only changes when a file is applied.</summary>
        public IReadOnlyList<AbilityEntry> Selectable() =>
            _selectable ??= [.. Abilities.Where(ability => !ability.Hidden)];
    }

    public readonly record struct AbilityEntry(string Id, Stance Stance, bool Hidden);
}

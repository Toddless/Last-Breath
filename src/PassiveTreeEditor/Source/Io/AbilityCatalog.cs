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

        public IReadOnlyList<string> Catalogs => [DataCatalog.Abilities];

        /// <summary>Every ability of the catalog, stance first then id, for stable picker order.</summary>
        public IReadOnlyList<AbilityEntry> Abilities => _abilities;

        public void Apply(string catalog, GameDataFile file)
        {
            AbilityDataRoot root = JsonConvert.DeserializeObject<AbilityDataRoot>(file.Json)
                                   ?? throw new InvalidOperationException($"Failed to deserialize ability data from {file.FileName}");

            foreach (AbilityBaseData ability in root.Abilities)
                _abilities.Add(new AbilityEntry(ability.Id, ability.Stance, ability.Hidden));

            _abilities.Sort(static (first, second) =>
            {
                int byStance = first.Stance.CompareTo(second.Stance);
                return byStance != 0 ? byStance : string.CompareOrdinal(first.Id, second.Id);
            });
        }

        /// <summary>Abilities a tree node may reference. Hidden ones are internal-cast only and, by
        /// their own data comment, never appear in trees.</summary>
        public List<AbilityEntry> Selectable()
        {
            List<AbilityEntry> selectable = [];
            selectable.AddRange(_abilities.Where(ability => !ability.Hidden));

            return selectable;
        }
    }

    public readonly record struct AbilityEntry(string Id, Stance Stance, bool Hidden);
}

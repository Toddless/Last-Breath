namespace Core.Battle.Skills
{
    using System;
    using System.Collections.Generic;
    using Data.GameData;
    using Newtonsoft.Json;

    /// <summary>On-disk shape of the passive catalog: explicit <c>JsonProperty</c> names, camelCase keys.</summary>
    public sealed class PassiveCatalogDto
    {
        [JsonProperty("passives")] public List<PassiveCatalogEntryDto> Passives { get; set; } = [];
    }

    /// <summary>One named passive: the id a node may write, and the fields its factory reads.</summary>
    public sealed class PassiveCatalogEntryDto
    {
        [JsonProperty("id")] public string Id { get; set; } = string.Empty;

        [JsonProperty("fields")] public List<string> Fields { get; set; } = [];
    }

    /// <summary>
    /// The passives a tree node may grant. Named entries come from data; the stat family is answered by
    /// its prefix instead, because an id under it is invented by the author and its fields are free.
    /// <para>The catalog is a copy of a registry that lives battle-side, where the authoring tool cannot
    /// reach it. A test in the game suite is what keeps the copy honest.</para>
    /// </summary>
    public sealed class PassiveSkillCatalog : IGameDataParticipant
    {
        private static readonly IReadOnlyList<string> s_noFields = [];

        private readonly Dictionary<string, IReadOnlyList<string>> _fieldsById = new(StringComparer.Ordinal);
        private readonly List<string> _ids = [];

        public IReadOnlyList<string> Catalogs => [DataCatalog.PassiveSkills];

        /// <summary>Every named id the catalog holds, in the order data wrote them.</summary>
        public IReadOnlyList<string> Ids => _ids;

        public bool IsEmpty => _ids.Count == 0;

        /// <summary>Empties the catalog so the next read starts from nothing — a catalog is a folder of
        /// files, and entries otherwise accumulate across reads of two different data roots.</summary>
        public void Reset()
        {
            _fieldsById.Clear();
            _ids.Clear();
        }

        public void Apply(string catalog, GameDataFile file)
        {
            PassiveCatalogDto root = JsonConvert.DeserializeObject<PassiveCatalogDto>(file.Json)
                                     ?? throw new InvalidOperationException($"Failed to read passive catalog from {file.FileName}");

            foreach (PassiveCatalogEntryDto entry in root.Passives) Add(entry);
        }

        /// <summary>Whether anything answers to this id: a named entry, or the stat family by its prefix.
        /// An id nobody knows is a node that will hand the player nothing.</summary>
        public bool Knows(string? id) =>
            StatPassiveGrammar.Owns(id) || (id is not null && _fieldsById.ContainsKey(id));

        /// <summary>The fields the id's factory reads, or nothing for the stat family, whose fields are
        /// stat lines the author writes freely.</summary>
        public IReadOnlyList<string> RequiredFields(string? id) =>
            id is not null && _fieldsById.TryGetValue(id, out IReadOnlyList<string>? fields) ? fields : s_noFields;

        /// <summary>The catalog as one document, for a reader outside the data pipeline.</summary>
        public static PassiveSkillCatalog Read(string json)
        {
            var catalog = new PassiveSkillCatalog();
            catalog.Apply(DataCatalog.PassiveSkills, new GameDataFile(DataCatalog.PassiveSkills, json));
            return catalog;
        }

        private void Add(PassiveCatalogEntryDto entry)
        {
            if (string.IsNullOrWhiteSpace(entry.Id) || !_fieldsById.TryAdd(entry.Id, entry.Fields)) return;

            _ids.Add(entry.Id);
        }
    }
}

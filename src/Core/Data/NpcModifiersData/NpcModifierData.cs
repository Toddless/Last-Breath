namespace Core.Data.NpcModifiersData
{
    using GameData;
    using Newtonsoft.Json;
    using Schema;

    /// <summary>What every npc modifier writes, whichever section it stands in. The flag saying whether it
    /// is unique is NOT here: each section writes it where its own author put it, and the order a record's
    /// keys stand in is the order the type declares them.</summary>
    public record NpcModifierData
    {
        [JsonProperty("id")] public string Id { get; init; } = string.Empty;

        /// <summary>The buff the modifier pulls onto its bearer: the parameter half of the modifier, whose
        /// numbers live in the buff catalog. A modifier naming no buff carries no numbers at all.</summary>
        [JsonProperty("npcBuffId")]
        [CatalogRef(DataCatalog.NpcBuffs)]
        public string NpcBuffId { get; init; } = string.Empty;

        [JsonProperty("weight")] public float Weight { get; init; }
        [JsonProperty("difficulty")] public float Difficulty { get; init; }

        /// <summary>Stamped by the parser from the section this entry sat in — the section's uniqueScope,
        /// carried down so the factory can hand it to the modifier without re-reading the file.
        /// Not a JSON field of the entry itself.</summary>
        [JsonIgnore] public Enums.NpcUniqueScope UniqueScope { get; init; }
    }
}

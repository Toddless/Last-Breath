namespace Core.Data.NpcSpawnRollsData
{
    using System.Collections.Generic;
    using Enums;
    using Newtonsoft.Json;
    using Schema;

    /// <summary>NpcSpawnRolls.json: the chance ladders deciding how many of an NPC's modifier and ability
    /// slots are filled, the rarity ladder scaling all of them at once, and how far "unique" reaches
    /// inside a section of the modifier catalog. All of it is one question — what a rolled NPC ends up
    /// wearing — asked of the modifier catalog, which holds only what there is to wear.</summary>
    public record SpawnRollsData
    {
        /// <summary>Json key of the map of modifier section to the reach of its uniqueness.</summary>
        public const string UniqueScopeSection = "uniqueScope";

        [JsonProperty("modifiers")][DictionaryKey(typeof(EntityType))] public Dictionary<string, SlotLadderData> Modifiers { get; init; } = [];
        [JsonProperty("abilities")][DictionaryKey(typeof(EntityType))] public Dictionary<string, SlotLadderData> Abilities { get; init; } = [];
        [JsonProperty("rarityMultipliers")][DictionaryKey(typeof(Rarity))] public Dictionary<string, float> RarityMultipliers { get; init; } = [];

        /// <summary>Section of the NpcModifiers catalog to how far uniqueness reaches inside it. Keyed by
        /// the section's own json name, which no markup can offer: the keys of a map are members of an
        /// enum or ids of a catalog, and a section key is neither. A key naming no section is refused
        /// where it is read.</summary>
        [JsonProperty(UniqueScopeSection)]
        public Dictionary<string, NpcUniqueScope> UniqueScope { get; init; } = [];
    }

    /// <summary>One ladder. Nullable on purpose: absent is a question the file has to answer, not a
    /// number to guess at — the only value that keeps the arithmetic running reads as "this slot is
    /// certain", which is what the catalog exists to end.</summary>
    public record SlotLadderData
    {
        [JsonProperty("firstSlotChance")] public float? FirstSlotChance { get; init; }
        [JsonProperty("nextSlotChance")] public float? NextSlotChance { get; init; }
        [JsonProperty("decay")] public float? Decay { get; init; }
    }
}

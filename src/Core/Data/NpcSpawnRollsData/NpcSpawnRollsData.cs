namespace Core.Data.NpcSpawnRollsData
{
    using System.Collections.Generic;
    using Enums;
    using Newtonsoft.Json;
    using Schema;

    /// <summary>NpcSpawnRolls.json: the chance ladders deciding how many of an NPC's modifier and ability
    /// slots are filled, and the rarity ladder scaling all of them at once.</summary>
    public record SpawnRollsData
    {
        [JsonProperty("modifiers")][DictionaryKey(typeof(EntityType))] public Dictionary<string, SlotLadderData> Modifiers { get; init; } = [];
        [JsonProperty("abilities")][DictionaryKey(typeof(EntityType))] public Dictionary<string, SlotLadderData> Abilities { get; init; } = [];
        [JsonProperty("rarityMultipliers")][DictionaryKey(typeof(Rarity))] public Dictionary<string, float> RarityMultipliers { get; init; } = [];
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

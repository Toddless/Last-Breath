namespace Core.Data.SaveData
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>The drops still lying on the world floor. Saving them is what makes loot the player's the
    /// moment it falls: an unsaved floor turned every reload into a way of losing what was not walked over
    /// yet.</summary>
    public class GroundItemsSaveData
    {
        [JsonProperty("items")] public List<GroundItemSaveData> Items { get; init; } = [];
    }

    /// <summary>One drop, written in the shape the bag writes its own (<see cref="InventoryItemSaveData"/>,
    /// units included) plus the spot it lies on. A map/scene id joins here when multiple maps exist.</summary>
    public class GroundItemSaveData
    {
        [JsonProperty("item")] public InventoryItemSaveData Item { get; init; } = new();

        [JsonProperty("x")] public float X { get; init; }

        [JsonProperty("y")] public float Y { get; init; }
    }
}

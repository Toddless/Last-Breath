namespace Core.Data.SaveData
{
    using Newtonsoft.Json;

    /// <summary>World position; a map/scene id joins here when multiple maps exist.</summary>
    public class PlayerPlacementSaveData
    {
        [JsonProperty("locationId")] public string LocationId { get; init; } = World.Locations.LocationCatalog.MainWorldId;
        [JsonProperty("rotation")] public float Rotation { get; init; }
        [JsonProperty("x")] public float X { get; init; }
        [JsonProperty("y")] public float Y { get; init; }
    }
}

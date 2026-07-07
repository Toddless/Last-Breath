namespace Core.Data.SaveData
{
    using Newtonsoft.Json;

    /// <summary>World position; a map/scene id joins here when multiple maps exist.</summary>
    public class PlayerPlacementSaveData
    {
        [JsonProperty("x")] public float X { get; init; }
        [JsonProperty("y")] public float Y { get; init; }
    }
}

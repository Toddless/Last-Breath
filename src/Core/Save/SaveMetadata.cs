namespace Core.Save
{
    using System;
    using Newtonsoft.Json;

    /// <summary>Slot list data: duplicated into meta.json so the UI never parses the full save.</summary>
    public class SaveMetadata
    {
        [JsonProperty("name")] public string Name { get; init; } = string.Empty;
        [JsonProperty("savedAtUtc")] public DateTime SavedAtUtc { get; init; }
        [JsonProperty("location")] public string Location { get; init; } = string.Empty;
        [JsonProperty("masteryLevel")] public int MasteryLevel { get; init; }
    }
}

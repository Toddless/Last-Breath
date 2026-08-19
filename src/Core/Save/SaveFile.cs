namespace Core.Save
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>One save slot payload: versioned envelope of independent sections, rewritten whole
    /// on every save. Scoped to a single project (<c>user://saves</c> is per-project), so it holds
    /// exactly what that project's registered participants captured.</summary>
    public class SaveFile
    {
        public const int CurrentFormatVersion = 1;

        [JsonProperty("formatVersion")] public int FormatVersion { get; init; } = CurrentFormatVersion;
        [JsonProperty("metadata")] public SaveMetadata Metadata { get; set; } = new();
        [JsonProperty("sections")] public Dictionary<string, SaveSection> Sections { get; init; } = [];
    }
}

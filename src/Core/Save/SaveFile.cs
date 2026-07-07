namespace Core.Save
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>
    /// One save slot payload: versioned envelope of independent sections. Battle/Crafting/Main
    /// write the SAME file, each owning its sections — unknown sections must survive a rewrite.
    /// </summary>
    public class SaveFile
    {
        public const int CurrentFormatVersion = 1;

        [JsonProperty("formatVersion")] public int FormatVersion { get; init; } = CurrentFormatVersion;
        [JsonProperty("metadata")] public SaveMetadata Metadata { get; set; } = new();
        [JsonProperty("sections")] public Dictionary<string, SaveSection> Sections { get; init; } = [];
    }
}

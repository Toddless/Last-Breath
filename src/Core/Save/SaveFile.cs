namespace Core.Save
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>
    /// One save slot payload: versioned envelope of independent sections, rewritten whole on every
    /// save. The file belongs to a single project — storage is rooted at the running project's own
    /// <c>user://saves</c> — so it holds exactly the sections that project's registered participants
    /// captured, and a section nobody captures any more is gone with the next save.
    /// </summary>
    public class SaveFile
    {
        public const int CurrentFormatVersion = 1;

        [JsonProperty("formatVersion")] public int FormatVersion { get; init; } = CurrentFormatVersion;
        [JsonProperty("metadata")] public SaveMetadata Metadata { get; set; } = new();
        [JsonProperty("sections")] public Dictionary<string, SaveSection> Sections { get; init; } = [];
    }
}

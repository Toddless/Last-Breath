namespace Core.Data.FormattingData
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>SharedData/Formatting/ParameterFormats.json — how parameter values LOOK in UI.</summary>
    public class ParameterFormatsData
    {
        [JsonProperty("parameters")] public List<ParameterFormatEntry> Parameters { get; init; } = [];
    }

    public class ParameterFormatEntry
    {
        [JsonProperty("parameter")] public string Parameter { get; init; } = string.Empty;

        /// <summary>"Percent" — data stores a fraction (0.5 = 50%), shown ×100 with a % sign; "Number" — as is.</summary>
        [JsonProperty("unit")] public string Unit { get; init; } = string.Empty;
    }
}

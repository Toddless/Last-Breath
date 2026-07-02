namespace Core.Data.AbilityData
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public record AbilityDataRoot
    {
        [JsonProperty("abilities")] public List<AbilityBaseData> Abilities { get; init; } = [];
    }
}

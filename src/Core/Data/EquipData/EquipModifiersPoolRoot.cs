namespace Core.Data.EquipData
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public record EquipModifiersPoolRoot
    {
        [JsonProperty("pools")] public List<EquipModifierPoolData> Root { get; init; } = [];
    }
}

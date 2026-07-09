namespace Core.Data.EquipData
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public record EquipModifierPoolData
    {
        [JsonProperty("id")] public string Id { get; init; } = string.Empty;
        [JsonProperty("modifiersPool")] public List<ItemModifier> ModifiersPool { get; init; } = [];
    }
}

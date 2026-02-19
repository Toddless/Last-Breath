namespace Core.Data.NpcModifiersData
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public class ModifiersData
    {
        [JsonProperty("mods")] public List<NpcModifiersData> Mods { get; init; } = [];
    }
}

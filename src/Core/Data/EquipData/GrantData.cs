namespace Core.Data.EquipData
{
    using System.Collections.Generic;
    using Enums;
    using GameData;
    using Newtonsoft.Json;
    using Schema;

    public record GrantData
    {
        [JsonProperty("kind")][EnumOf(typeof(GrantKind))] public string Kind { get; init; } = string.Empty;

        /// <summary>What the granted behaviour is: a passive skill or an effect, by the id it is written
        /// under in its own catalog. A <see cref="GrantKind.Modifier"/> grant names no record — its id is
        /// the label its own lines are minted under.</summary>
        [JsonProperty("id")]
        [CatalogRef(DataCatalog.PassiveSkills)]
        [CatalogRef(DataCatalog.Effects)]
        public string Id { get; init; } = string.Empty;

        [JsonProperty("modifiers")] public List<ItemModifier> Modifiers { get; init; } = [];

        /// <summary>Numeric parameters of the granted behavior ("percent": 0.15) — balance lives in data.</summary>
        [JsonProperty("properties")] public Dictionary<string, float> Properties { get; init; } = [];
    }
}

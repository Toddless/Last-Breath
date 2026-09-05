namespace Core.Data.NpcModifiersData
{
    using GameData;
    using Newtonsoft.Json;
    using Schema;

    public record ItemEffectData : NpcModifierData
    {
        /// <summary>The grant every equipment piece of the kill comes out carrying, drawn from the same
        /// catalog an ordinary kill rolls its bonus grant out of.</summary>
        [JsonProperty("effectId")]
        [CatalogRef(DataCatalog.ItemEffects)]
        public string EffectId { get; init; } = string.Empty;

        [JsonProperty("isUnique")] public bool IsUnique { get; init; }
    }
}

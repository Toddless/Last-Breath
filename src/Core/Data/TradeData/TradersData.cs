namespace Core.Data.TradeData
{
    using System.Collections.Generic;
    using CraftingData;
    using Enums;
    using GameData;
    using Newtonsoft.Json;
    using Schema;

    public record TradersData
    {
        [JsonProperty("traders")] public List<TraderData> Traders { get; init; } = [];
    }

    /// <summary>One trader: an authored catalog (id + count + chance per restock) plus optional
    /// random equip slots minted through the real item pipeline (hybrid stock, Todd 2026-07-24).</summary>
    public record TraderData
    {
        [JsonProperty("id")] public string Id { get; init; } = string.Empty;

        /// <summary>Faction whose standing prices the shop (reputation multipliers + perks).</summary>
        [JsonProperty("fraction")][EnumOf(typeof(Fractions))] public string Fraction { get; init; } = string.Empty;

        /// <summary>Game minutes between restocks (at default clock speed one real second = one game minute).</summary>
        [JsonProperty("restockGameMinutes")] public float RestockGameMinutes { get; init; } = 1440f;

        [JsonProperty("catalog")] public List<TraderCatalogEntryData> Catalog { get; init; } = [];

        [JsonProperty("randomEquip")] public TraderRandomEquipData? RandomEquip { get; init; }
    }

    public record TraderCatalogEntryData
    {
        /// <summary>Names one thing to put on the shelf, out of every catalog a sellable thing is written
        /// in: the shop resolves an equipment template through the minter and everything else out of the
        /// item store, so any one of them knowing the id fills the seat. In the resources that is the two
        /// sections holding things — a material category is what a resource belongs to, never a good.</summary>
        [CatalogRef(DataCatalog.EquipItems)]
        [CatalogRef(DataCatalog.Items)]
        [CatalogRef(DataCatalog.Recipes)]
        [CatalogRef(DataCatalog.Resources, Section = ResourcesData.UpgradeResourcesSection)]
        [CatalogRef(DataCatalog.Resources, Section = ResourcesData.CraftingResourcesSection)]
        [JsonProperty("itemId")] public string ItemId { get; init; } = string.Empty;
        [JsonProperty("count")] public int Count { get; init; } = 1;

        /// <summary>Chance the entry appears in a given restock; 1 = always.</summary>
        [JsonProperty("chance")] public float Chance { get; init; } = 1f;
    }

    public record TraderRandomEquipData
    {
        [JsonProperty("count")] public int Count { get; init; }

        /// <summary>Rarity roll weights for the minted pieces (keys = Rarity names).</summary>
        [JsonProperty("rarityWeights")][DictionaryKey(typeof(Rarity))] public Dictionary<string, float> RarityWeights { get; init; } = [];
    }
}

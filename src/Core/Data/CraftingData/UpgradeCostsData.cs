namespace Core.Data.CraftingData
{
    using System.Collections.Generic;
    using Enums;
    using GameData;
    using Newtonsoft.Json;
    using Schema;

    /// <summary>The three sections of the upgrade costs file, one per operation that is paid for. Each is
    /// read under its own constant rather than under a literal repeated by whoever describes the file — a
    /// section renamed would otherwise move the key and leave the other spelling behind, in silence.</summary>
    public record UpgradeCostsData
    {
        /// <summary>Json key of the section holding what a sharpening attempt costs.</summary>
        public const string UpgradeSection = "upgrade";

        /// <summary>Json key of the section holding what rerolling one line costs.</summary>
        public const string RecraftSection = "recraft";

        /// <summary>Json key of the section holding what ascension costs.</summary>
        public const string AscendSection = "ascend";

        [JsonProperty(UpgradeSection)] public List<CategoryRequirementsData> Upgrade { get; init; } = [];
        [JsonProperty(RecraftSection)] public List<CategoryRequirementsData> Recraft { get; init; } = [];
        [JsonProperty(AscendSection)] public List<CategoryRequirementsData> Ascend { get; init; } = [];
    }

    public record CategoryRequirementsData
    {
        [JsonProperty("category")][EnumOf(typeof(EquipmentCategory))] public string Category { get; init; } = string.Empty;
        [JsonProperty("requirements")] public List<UpgradeRequirementData> Requirements { get; init; } = [];
    }

    /// <summary>One cost line: id/amount are the defaults for every rarity; byRarity overrides either
    /// half per item rarity (rarity dimension of the upgrade/recraft cost schema).</summary>
    public record UpgradeRequirementData
    {
        [JsonProperty("type")][EnumOf(typeof(RequirementType))] public string Type { get; init; } = string.Empty;

        /// <summary>The resource the line is paid with: the runes and dusts of the sharpening section, or
        /// the ore and gems a reforge is paid in. Absent when every rarity names its own, which is what
        /// the empty answer stands for.</summary>
        [JsonProperty("id")]
        [CatalogRef(DataCatalog.Resources, Section = ResourcesData.UpgradeResourcesSection, AllowEmpty = true)]
        [CatalogRef(DataCatalog.Resources, Section = ResourcesData.CraftingResourcesSection)]
        public string? Id { get; init; }

        [JsonProperty("amount")] public int? Amount { get; init; }
        [JsonProperty("byRarity")][DictionaryKey(typeof(Rarity))] public Dictionary<string, RarityRequirementOverrideData>? ByRarity { get; init; }
    }

    public record RarityRequirementOverrideData
    {
        /// <summary>The resource this rarity pays in instead of the line's own; absent — which is what the
        /// empty answer stands for — when only the amount differs.</summary>
        [JsonProperty("id")]
        [CatalogRef(DataCatalog.Resources, Section = ResourcesData.UpgradeResourcesSection, AllowEmpty = true)]
        [CatalogRef(DataCatalog.Resources, Section = ResourcesData.CraftingResourcesSection)]
        public string? Id { get; init; }

        [JsonProperty("amount")] public int? Amount { get; init; }
    }
}

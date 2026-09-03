namespace Core.Data.CraftingData
{
    using System.Collections.Generic;
    using Enums;
    using GameData;
    using Newtonsoft.Json;
    using Schema;

    /// <summary>CraftingAdditives.json: what each optional crafting resource does.</summary>
    public record CraftingAdditivesData
    {
        [JsonProperty("additives")] public List<CraftingAdditiveEntry> Additives { get; init; } = [];
    }

    public record CraftingAdditiveEntry
    {
        /// <summary>The resource this record speaks for, and the whole of its identity: an additive is a
        /// resource the player drops into an optional slot, and the record says what that does.</summary>
        [JsonProperty("resourceId")]
        [CatalogRef(DataCatalog.Resources, Section = ResourcesData.UpgradeResourcesSection)]
        [CatalogRef(DataCatalog.Resources, Section = ResourcesData.CraftingResourcesSection)]
        public string ResourceId { get; init; } = string.Empty;

        /// <summary>Flat bonus to the upgrade success chance (0.1 = +10%).</summary>
        [JsonProperty("upgradeChanceBonus")] public float UpgradeChanceBonus { get; init; }

        /// <summary>Chance that a successful upgrade grants a second level.</summary>
        [JsonProperty("extraUpgradeLevelChance")] public float ExtraUpgradeLevelChance { get; init; }

        /// <summary>Modifier pool mixed into a recraft roll while this additive is used.</summary>
        [JsonProperty("recraftPoolId")]
        [CatalogRef(DataCatalog.ModifierPools, AllowEmpty = true)]
        public string? RecraftPoolId { get; init; }

        /// <summary>Creation rune: the guaranteed minimum rarity of the created item (a <see cref="Core.Enums.Rarity"/>
        /// name, parsed strictly — a typo drops the whole entry).</summary>
        [JsonProperty("minRarity")][EnumOf(typeof(Rarity))] public string? MinRarity { get; init; }
    }
}

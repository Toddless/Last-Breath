namespace Core.Data.NpcModifiersData
{
    using System.Collections.Generic;
    using CraftingData;
    using GameData;
    using Newtonsoft.Json;
    using Schema;

    public record GuaranteedItemsData : NpcModifierData
    {
        /// <summary>What the kill hands over on top of everything it rolls. Answered from exactly where a
        /// rolled drop is: the seat of a loot table and this list name the same things.</summary>
        [JsonProperty("items")]
        [CatalogRef(DataCatalog.EquipItems)]
        [CatalogRef(DataCatalog.Items)]
        [CatalogRef(DataCatalog.Recipes)]
        [CatalogRef(DataCatalog.Resources, Section = ResourcesData.UpgradeResourcesSection)]
        [CatalogRef(DataCatalog.Resources, Section = ResourcesData.CraftingResourcesSection)]
        public List<string> Items { get; init; } = [];

        [JsonProperty("isUnique")] public bool IsUnique { get; init; }
    }
}

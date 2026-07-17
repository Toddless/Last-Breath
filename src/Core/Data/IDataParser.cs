namespace Core.Data
{
    using System.Collections.Generic;
    using Enums;
    using Items;
    using LootTable;
    using Modifiers;

    /// <summary>
    /// Parses one JSON document of the item/crafting domain into domain objects. Synchronous by
    /// design — there is nothing to await; the providers merge the per-file results themselves.
    /// </summary>
    public interface IDataParser
    {
        LootTablesParseResult ParseLootTables(string json);
        LootConfigurationParseResult ParseLootConfiguration(string json);
        Dictionary<string, Dictionary<string, int>> ParseEquipItemResources(string json);
        Dictionary<string, List<IModifierDescriptor>> ParseEquipItemModifierPools(string json);
        List<IItem> ParseItems(string json);
        List<EquipItemBlueprint> ParseEquipItems(string json);
        List<IItem> ParseRecipes(string json);
        List<IItem> ParseResources(string json);
        /// <summary>Cost lines stay unresolved (defaults + per-rarity overrides): the provider resolves
        /// them against a concrete item rarity at read time.</summary>
        Dictionary<CraftingMode, Dictionary<EquipmentCategory, List<Crafting.CostRequirement>>> ParseUpgradeCosts(string json);
    }
}

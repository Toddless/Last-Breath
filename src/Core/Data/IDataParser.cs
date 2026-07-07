namespace Core.Data
{
    using System.Collections.Generic;
    using Enums;
    using Interfaces;
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
        Dictionary<string, Dictionary<string, int>> ParseEquipItemResources(string json);
        Dictionary<string, List<IModifier>> ParseEquipItemModifierPools(string json);
        List<IItem> ParseItems(string json);
        List<IItem> ParseEquipItems(string json);
        List<IItem> ParseRecipes(string json);
        List<IItem> ParseResources(string json);
        Dictionary<CraftingMode, Dictionary<EquipmentCategory, List<IRequirement>>> ParseUpgradeCosts(string json);
    }
}

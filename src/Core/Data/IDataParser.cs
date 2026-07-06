namespace Core.Data
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Enums;
    using Interfaces;
    using Interfaces.Items;
    using LootTable;
    using Modifiers;

    public interface IDataParser
    {
        Task ParseLootTables(string json,
            ref Dictionary<Fractions, List<LootTableTierData>> fractionsTables,
            ref Dictionary<EntityType, List<LootTableTierData>> entityTypeTables,
            ref Dictionary<string, List<LootTableTierData>> individualTables,
            ref List<LootTableTierData> basicTable);

        Task<Dictionary<string, Dictionary<string, int>>> ParseEquipItemResources(string json);
        Task ParseEquipItemModifierPools(string json, ref Dictionary<string, List<IModifier>> equipItemModifierPools);
        Task<List<IItem>> ParseItems(string json);
        Task<List<IItem>> ParseEquipItems(string json);
        Task<List<IItem>> ParseRecipes(string json);
        Task<List<IItem>> ParseResources(string json);
        Task<Dictionary<CraftingMode, Dictionary<EquipmentCategory, List<IRequirement>>>> ParseUpgradeCosts(string json);
    }
}

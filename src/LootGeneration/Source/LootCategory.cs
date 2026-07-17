namespace LootGeneration.Source
{
    using Core.Crafting;
    using Core.Items;

    /// <summary>
    /// Coarse grouping of ground loot: a click on a Resource/Recipe drop picks up the whole
    /// category, Equipment (unique rolls) is always picked up piece by piece.
    /// </summary>
    public enum LootCategory
    {
        Equipment,
        Resource,
        Recipe,
        Other,
    }

    public static class LootCategoryResolver
    {
        public static LootCategory Resolve(IItem item) => item switch
        {
            IEquipItem => LootCategory.Equipment,
            ICraftingResource => LootCategory.Resource,
            ICraftingRecipe => LootCategory.Recipe,
            _ => LootCategory.Other,
        };
    }
}

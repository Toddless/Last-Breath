namespace Core.Data
{
    using System.Collections.Generic;
    using Crafting;
    using Enums;
    using Godot;
    using Interfaces;
    using Items;
    using Modifiers;

    public interface IItemDataProvider
    {
        IItem CopyItem(string id);
        IEnumerable<IItem> GetAllResources();
        IEnumerable<ICraftingRecipe> GetCraftingRecipes();
        Texture2D? GetItemIcon(string id);
        ICraftingRecipe GetRecipe(string recipeId);
        List<IRequirement> GetRecipeRequirements(string id);
        string GetRecipeResultItemId(string recipeId);
        IReadOnlyList<IModifier> GetResourceModifiers(string id);
        bool IsItemHasTag(string id, string tag);
        List<IModifier> GetEquipItemModifierPool(string id);
        Dictionary<string, int> GetEquipItemResources(string itemId);
        List<IModifier> GetEquipItemBaseModifierPool(string id);
        IReadOnlyList<IRequirement> GetUpgradeCost(EquipmentCategory category);
        IReadOnlyList<IRequirement> GetRecraftCost(EquipmentCategory category);
    }
}

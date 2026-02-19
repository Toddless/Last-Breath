namespace Core.Data
{
    using Godot;
    using Interfaces.Items;
    using Interfaces.Crafting;
    using System.Collections.Generic;
    using Modifiers;

    public interface IItemDataProvider
    {
        IItem CopyItem(string id);
        IEnumerable<IItem> GetAllResources();
        IEnumerable<ICraftingRecipe> GetCraftingRecipes();
        Texture2D? GetItemIcon(string id);
        ICraftingRecipe GetRecipe(string recipeId);
        List<IResourceRequirement> GetRecipeRequirements(string id);
        string GetRecipeResultItemId(string recipeId);
        IReadOnlyList<IModifier> GetResourceModifiers(string id);
        bool IsItemHasTag(string id, string tag);
        void LoadData();
        List<IModifier> GetEquipItemModifierPool(string id);
        Dictionary<string, int> GetEquipItemResources(string itemId);
    }
}

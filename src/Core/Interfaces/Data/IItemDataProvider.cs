namespace Core.Interfaces.Data
{
    using System.Collections.Generic;
    using Core.Interfaces;
    using Core.Interfaces.Crafting;
    using Core.Interfaces.Items;
    using Godot;

    public interface IItemDataProvider
    {
        IItem? CopyBaseItem(string id);
        IEnumerable<IItem> GetAllResources();
        IEnumerable<ICraftingRecipe> GetCraftingRecipes();
        List<IModifier> GetItemBaseStats(string id);
        Texture2D? GetItemIcon(string id);
        List<IResourceRequirement> GetRecipeRequirements(string id);
        string GetRecipeResultItemId(string recipeId);
        IReadOnlyList<IMaterialModifier> GetResourceModifiers(string id);
        void LoadData();
    }
}

namespace Core.Interfaces.Data
{
    using Godot;
    using Items;
    using Crafting;
    using System.Collections.Generic;

    public interface IItemDataProvider
    {
        IItem CopyBaseItem(string id);
        IEnumerable<IItem> GetAllResources();
        IEnumerable<ICraftingRecipe> GetCraftingRecipes();
        Texture2D? GetItemIcon(string id);
        ICraftingRecipe GetRecipe(string recipeId);
        List<IResourceRequirement> GetRecipeRequirements(string id);
        string GetRecipeResultItemId(string recipeId);
        IReadOnlyList<IMaterialModifier> GetResourceModifiers(string id);
        bool IsItemHasTag(string id, string tag);
        void LoadData();
    }
}

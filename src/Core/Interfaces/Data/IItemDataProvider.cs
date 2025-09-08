namespace Core.Interfaces.Data
{
    using System.Collections.Generic;
    using Core.Interfaces.Crafting;
    using Core.Interfaces.Items;
    using Godot;

    public interface IItemDataProvider
    {
        static abstract IItemDataProvider? Instance { get; }

        IItem? CopyBaseItem(string id);
        IEnumerable<IItem> GetAllResources();
        IEnumerable<ICraftingRecipe> GetCraftingRecipes();
        List<string> GetItemBaseStats(string id);
        string GetItemDisplayName(string id);
        Texture2D? GetItemIcon(string id);
        int GetItemMaxStackSize(string id);
        ItemStats GetItemStats(string id);
        string GetItemType(string id);
        IReadOnlyList<IMaterialModifier> GetResourceModifiers(string id);
        void LoadData();
    }
}

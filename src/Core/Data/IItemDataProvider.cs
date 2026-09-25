namespace Core.Data
{
    using System.Collections.Generic;
    using Crafting;
    using Enums;
    using Godot;
    using Interfaces;
    using Items;
    using Modifiers;

    public interface IItemDataProvider : IEquipBlueprintProvider
    {
        /// <summary>Independent copy of a plain (non-equip) template. Equip templates live as
        /// blueprints and can only be born through <see cref="Items.IEquipItemMinter"/> — an equip id
        /// here is a "not found" by design.</summary>
        IItem CopyItem(string id);
        IEnumerable<IItem> GetAllResources();
        IEnumerable<ICraftingRecipe> GetCraftingRecipes();
        Texture2D? GetItemIcon(string id);
        ICraftingRecipe GetRecipe(string recipeId);
        List<IRequirement> GetRecipeRequirements(string id);
        string GetRecipeResultItemId(string recipeId);
        IReadOnlyList<IModifierDescriptor> GetResourceDescriptors(string id);
        bool IsItemHasTag(string id, string tag);

        /// <summary>Whether the id names a material category ("Category_Metal") — the membership
        /// source of truth is the resources' own material data, not free-form tags.</summary>
        bool IsMaterialCategory(string id);

        /// <summary>Every crafting resource whose material belongs to the category. The candidate
        /// list of a category requirement slot.</summary>
        IReadOnlyList<string> GetResourceIdsInCategory(string categoryId);
        /// <summary>Rollable affix pool of one item id (or an additive/mythic pool id) as immutable descriptors.</summary>
        IReadOnlyList<IModifierDescriptor> GetEquipItemModifierPool(string id);
        /// <summary>Family pool shared by every item of the piece (the id prefix before the first underscore).</summary>
        IReadOnlyList<IModifierDescriptor> GetEquipItemBaseModifierPool(string id);
        /// <summary>Sharpening cost resolved for the item's rarity (rune counts live in data, not code).</summary>
        IReadOnlyList<IRequirement> GetUpgradeCost(EquipmentCategory category, Rarity rarity);

        /// <summary>Recraft cost resolved for the item's rarity (dust + the category/rarity main resource).</summary>
        IReadOnlyList<IRequirement> GetRecraftCost(EquipmentCategory category, Rarity rarity);

        /// <summary>Ascension is Legendary-only by rule, so the cost carries no rarity dimension.</summary>
        IReadOnlyList<IRequirement> GetAscendCost(EquipmentCategory category);
    }
}

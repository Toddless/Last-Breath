namespace Core.Crafting
{
    using System.Collections.Generic;
    using Enums;
    using Interfaces;

    public interface ICraftingRecipe : IIdentifiable, IDisplayable, ITaggable
    {
        string ResultItemId { get; }

        /// <summary>Mastery level (earned + bonus) at which the recipe becomes known by itself;
        /// null = never — such a recipe is learnable only from its scroll item.</summary>
        int? UnlockAtMastery { get; }

        List<IRequirement> Requirements { get; set; }
        ItemType ItemType { get; }
        string[] OptionalResourceCategories { get; }
    }
}

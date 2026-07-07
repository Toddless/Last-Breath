namespace Core.Crafting
{
    using System.Collections.Generic;
    using Enums;
    using Interfaces;

    public interface ICraftingRecipe : IIdentifiable, IDisplayable, ITaggable
    {
        string ResultItemId { get; }
        bool IsOpened { get; }
        List<IRequirement> Requirements { get; set; }
        ItemType ItemType { get; }
        string[] OptionalResourceCategories { get; }
    }
}

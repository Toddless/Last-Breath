namespace Core.Interfaces.Crafting
{
    using System.Collections.Generic;
    using Enums;

    public interface ICraftingRecipe : IIdentifiable, IDisplayable, ITaggable
    {
        string ResultItemId {  get; }
        bool IsOpened { get; }
        List<IResourceRequirement> MainResource { get; set; }
        ItemType ItemType { get; }
    }
}

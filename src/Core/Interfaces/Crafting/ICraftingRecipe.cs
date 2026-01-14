namespace Core.Interfaces.Crafting
{
    using System.Collections.Generic;

    public interface ICraftingRecipe : IIdentifiable, IDisplayable, ITaggable
    {
        string ResultItemId {  get; }
        bool IsOpened { get; }
        List<IResourceRequirement> MainResource { get; set; }
    }
}

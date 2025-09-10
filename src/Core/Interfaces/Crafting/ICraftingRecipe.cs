namespace Core.Interfaces.Crafting
{
    using System.Collections.Generic;

    public interface ICraftingRecipe
    {
        string Id { get; }
        string ResultItemId {  get; }
        string Description { get; }
        string DisplayName { get; }
        bool IsOpened { get; }
        string[] Tags { get; }
        List<IRecipeRequirement> MainResource { get; }
    }
}

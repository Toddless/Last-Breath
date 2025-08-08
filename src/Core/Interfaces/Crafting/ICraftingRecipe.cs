namespace Core.Interfaces.Crafting
{
    using System.Collections.Generic;

    public interface ICraftingRecipe
    {
        string Description { get; }
        string Name { get; }
        List<IRecipeRequirement> MainResource { get; }
        List<IRecipeRequirement> OptionalResources { get; }
    }
}

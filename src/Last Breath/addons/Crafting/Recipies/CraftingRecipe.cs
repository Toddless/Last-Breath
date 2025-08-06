namespace LastBreath.Addons.Crafting.Recipies
{
    using Core.Interfaces.Crafting;
    using Godot;
    using Godot.Collections;
    using LastBreath.addons.Crafting.Recipies;

    [GlobalClass]
    public partial class CraftingRecipe : Resource
    {
        [Export] private Array<RecipeRequirement> _mainResources = [];
        [Export] private Array<RecipeRequirement> _optionalResources = [];

        public System.Collections.Generic.List<IRecipeRequirement> MainResource => [.. _mainResources];
        public System.Collections.Generic.List<IRecipeRequirement> OptionalResources => [.. _optionalResources];
    }
}

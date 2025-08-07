namespace LastBreath.Addons.Crafting.Recipies
{
    using Core.Interfaces.Crafting;
    using Core.Interfaces.Items;
    using Godot;
    using Godot.Collections;
    using LastBreath.addons.Crafting.Recipies;
    using LastBreath.Script.Items;

    [GlobalClass]
    public partial class CraftingRecipe : Resource
    {
        [Export] private Array<RecipeRequirement> _mainResources = [];
        [Export] private Array<RecipeRequirement> _optionalResources = [];
        [Export] private Item? _item;
        public IItem? Item => _item;

        public System.Collections.Generic.List<IRecipeRequirement> MainResource => [.. _mainResources];
        public System.Collections.Generic.List<IRecipeRequirement> OptionalResources => [.. _optionalResources];
    }
}

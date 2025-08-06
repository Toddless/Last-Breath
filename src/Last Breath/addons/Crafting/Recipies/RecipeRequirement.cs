namespace LastBreath.addons.Crafting.Recipies
{
    using Godot;
    using Core.Interfaces.Crafting;
    using LastBreath.Addons.Crafting;
    using Core.Enums;

    [GlobalClass]
    public partial class RecipeRequirement : Resource, IRecipeRequirement
    {
        [Export] private CraftingResource? _resource;
        [Export] public int Quantity { get; private set; }
        [Export] public ResourceCategory Category { get; private set; }
        public ICraftingResource? Resource => _resource;
    }
}

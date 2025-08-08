namespace LastBreath.addons.Crafting.Recipies
{
    using Godot;
    using Core.Interfaces.Crafting;

    [GlobalClass]
    public partial class RecipeRequirement : Resource, IRecipeRequirement
    {
        [Export] public int Amount { get; set; } = 1;
        [Export] public string CraftingResourceId { get; private set; } = string.Empty;

        public bool Matches(ICraftingResource resource)
        {
            if (!string.IsNullOrWhiteSpace(CraftingResourceId) && resource.Id == CraftingResourceId) return true;
            if (resource.Quantity >= Amount) return true;
            return false;
        }

    }
}

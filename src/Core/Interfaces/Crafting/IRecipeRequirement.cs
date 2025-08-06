namespace Core.Interfaces.Crafting
{
    public interface IRecipeRequirement
    {
        int Quantity { get; }
        ICraftingResource? Resource { get; }
    }
}

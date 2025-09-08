namespace Core.Interfaces.Crafting
{
    public interface IRecipeRequirement
    {
        int Amount { get; }
        string CraftingResourceId { get; }
    }
}

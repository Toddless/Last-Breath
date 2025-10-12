namespace Core.Interfaces.Mediator.Events
{
    public record OpenCraftingWindowEvent(string Id, bool IsRecipe = true) : IEvent { }
}

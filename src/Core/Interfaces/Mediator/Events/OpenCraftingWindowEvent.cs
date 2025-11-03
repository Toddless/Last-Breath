namespace Core.Interfaces.Mediator.Events
{
    public record OpenCraftingWindowEvent(string Id, bool IsItem = true) : IEvent { }
}

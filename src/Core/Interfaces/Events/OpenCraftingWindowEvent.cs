namespace Core.Interfaces.Events
{
    public record OpenCraftingWindowEvent(string Id, bool IsItem = true) : IEvent { }
}

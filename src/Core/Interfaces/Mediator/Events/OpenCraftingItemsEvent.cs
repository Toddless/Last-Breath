namespace Core.Interfaces.Mediator.Events
{
    public record OpenCraftingItemsEvent(string ItemId) : IEvent
    {
    }
}

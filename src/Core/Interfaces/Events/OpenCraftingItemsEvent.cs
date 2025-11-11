namespace Core.Interfaces.Events
{
    public record OpenCraftingItemsEvent(string ItemId) : IEvent
    {
    }
}

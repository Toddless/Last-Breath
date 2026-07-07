namespace Core.Events
{
    public record OpenCraftingItemsMessage(string ItemId) : IMessage
    {
    }
}

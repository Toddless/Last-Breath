namespace Core.Interfaces.Events
{
    public record OpenCraftingItemsMessage(string ItemId) : IMessage
    {
    }
}

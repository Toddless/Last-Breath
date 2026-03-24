namespace Core.Interfaces.Events
{
    public record OpenCraftingWindowMessage(string Id, bool IsItem = true) : IMessage { }
}

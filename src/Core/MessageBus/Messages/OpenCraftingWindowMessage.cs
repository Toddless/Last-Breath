namespace Core.MessageBus.Messages
{
    using Enums;
    using Events;

    public record OpenCraftingWindowMessage(string Id, bool IsItem = true, CraftingMode CraftingMode = CraftingMode.Create) : IMessage;
}

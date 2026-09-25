namespace Core.MessageBus.Messages
{
    using Enums;

    public record OpenCraftingWindowMessage(string Id, bool IsItem = true, CraftingMode CraftingMode = CraftingMode.Create) : IMessage;
}

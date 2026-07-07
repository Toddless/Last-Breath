namespace Core.Events
{
    using Enums;

    public record OpenCraftingWindowMessage(string Id, bool IsItem = true, CraftingMode CraftingMode = CraftingMode.Create) : IMessage;
}

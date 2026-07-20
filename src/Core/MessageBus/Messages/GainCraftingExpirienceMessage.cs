namespace Core.MessageBus.Messages
{
    using Enums;

    public record GainCraftingExpirienceMessage(CraftingMode Action, Rarity ItemRarity) : IMessage
    {
    }
}

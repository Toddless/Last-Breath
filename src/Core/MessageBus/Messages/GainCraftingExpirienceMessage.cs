namespace Core.MessageBus.Messages
{
    using Enums;
    using Events;

    public record GainCraftingExpirienceMessage(CraftingMode Action, Rarity ItemRarity) : IMessage
    {
    }
}

namespace Core.Events
{
    using Enums;

    public record GainCraftingExpirienceMessage(CraftingMode Action, Rarity ItemRarity) : IMessage
    {
    }
}

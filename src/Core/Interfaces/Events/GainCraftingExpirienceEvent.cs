namespace Core.Interfaces.Events
{
    using Enums;

    public record GainCraftingExpirienceEvent(CraftingMode Action, Rarity ItemRarity) : IEvent
    {
    }
}

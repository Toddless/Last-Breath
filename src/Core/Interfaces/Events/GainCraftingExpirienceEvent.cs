namespace Core.Interfaces.Events
{
    using Core.Enums;

    public record GainCraftingExpirienceEvent(CraftingMode Action, Rarity ItemRarity) : IEvent
    {
    }
}

namespace LootGeneration.Source
{
    using Core.Events;
    using Core.Items;

    /// <summary>Fired after an item left the floor and entered an inventory.</summary>
    public record ItemPickedUpEvent(IItem Item, int Quantity) : IGameEvent;
}

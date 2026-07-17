namespace LootGeneration.Source
{
    using Core.Events;

    /// <summary>Fired when a drop lands on the floor and becomes pickable (scavenger AI hook).</summary>
    public record ItemDroppedEvent(ItemOnGround Drop) : IGameEvent;
}

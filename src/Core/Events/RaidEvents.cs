namespace Core.Events
{
    using Enums;
    using Godot;

    /// <summary>A faction at Hatred standing sent a squad after the player.</summary>
    public record RaidStartedEvent(Fractions Faction, int Size, Vector2 Origin) : IGameEvent;

    /// <summary>The raid is over: everyone died, or the survivors left after the timeout.</summary>
    public record RaidEndedEvent(Fractions Faction) : IGameEvent;
}

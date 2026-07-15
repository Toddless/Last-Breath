namespace Core.Events
{
    using Enums;
    using Godot;

    /// <summary>
    /// A defeated body rose as undead. The original spawn point stops owning it (the rising
    /// is wild) and generates a replacement of the original faction.
    /// </summary>
    public record NpcFactionChangedEvent(string InstanceId, string NpcId, Fractions From, Fractions To, Vector2 Position) : IGameEvent;
}

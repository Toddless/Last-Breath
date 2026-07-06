namespace Core.Interfaces.Events.GameEvents
{
    using Godot;

    /// <summary>
    /// A body was burned — this NPC is permanently gone. The owning spawn point frees the
    /// slot and generates a replacement; the population service releases the global reservation.
    /// </summary>
    public record NpcFinalDeathEvent(string InstanceId, string NpcId, Vector2 Position) : IGameEvent;
}

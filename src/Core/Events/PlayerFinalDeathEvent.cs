namespace Core.Events
{
    /// <summary>The player's corpse was burned — game over. The UI layer shows the death screen.</summary>
    public record PlayerFinalDeathEvent : IGameEvent;
}

namespace Core.Events.GameEvents
{
    public record PlayerMaxManaChangesEvent(float Value) : IBattleEvent, IGameEvent;
}

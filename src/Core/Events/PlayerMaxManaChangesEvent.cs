namespace Core.Events
{
    public record PlayerMaxManaChangesEvent(float Value) : IBattleEvent, IGameEvent;
}

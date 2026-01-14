namespace Core.Interfaces.Events.GameEvents
{
    public record PlayerMaxManaChangesEvent(float Value) : IBattleEvent, IGameEvent;
}

namespace Core.Interfaces.Events.GameEvents
{
    public record PlayerMaxManaChangesGameEvent(float Value) : IBattleEvent, IGameEvent;
}

namespace Core.Events.GameEvents
{
    using Entity;

    public record EntityHealthChangesEvent(IFightable Entity, float Value) : IGameEvent, IBattleEvent;
}

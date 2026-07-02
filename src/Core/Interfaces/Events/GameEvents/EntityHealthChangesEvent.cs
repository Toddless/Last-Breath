namespace Core.Interfaces.Events.GameEvents
{
    using Entity;

    public record EntityHealthChangesEvent(IFightable Entity, float Value) : IGameEvent, IBattleEvent;
}

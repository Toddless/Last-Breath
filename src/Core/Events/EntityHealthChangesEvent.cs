namespace Core.Events
{
    using Entity;

    public record EntityHealthChangesEvent(IFightable Entity, float Value) : IGameEvent, IBattleEvent;
}

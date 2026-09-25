namespace Core.Events
{
    using Entity;

    public record PlayerHealthChangesEvent(IFightable Player, float Value) : IGameEvent, IBattleEvent;
}

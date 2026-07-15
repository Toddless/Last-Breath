namespace Core.Events
{
    using Entity;

    public record PlayerMaxHealthChanges(IFightable Player, float Value) : IGameEvent, IBattleEvent;
}

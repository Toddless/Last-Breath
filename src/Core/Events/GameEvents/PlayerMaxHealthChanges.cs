namespace Core.Events.GameEvents
{
    using Entity;

    public record PlayerMaxHealthChanges(IFightable Player, float Value) : IGameEvent, IBattleEvent;
}

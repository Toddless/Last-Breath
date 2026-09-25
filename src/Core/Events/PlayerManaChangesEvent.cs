namespace Core.Events
{
    using Entity;

    public record PlayerManaChangesEvent(IFightable Player, float Value) : IGameEvent, IBattleEvent;
}

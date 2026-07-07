namespace Core.Events.GameEvents
{
    using Entity;

    public record PlayerManaChangesEvent(IFightable Player, float Value) : IGameEvent, IBattleEvent;
}

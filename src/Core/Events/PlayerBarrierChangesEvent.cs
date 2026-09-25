namespace Core.Events
{
    using Entity;

    public record PlayerBarrierChangesEvent(IFightable Player, float Value) : IGameEvent, IBattleEvent;
}

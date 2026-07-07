namespace Core.Events.GameEvents
{
    using Entity;

    public record PlayerBarrierChangesEvent(IFightable Player, float Value) : IGameEvent, IBattleEvent;
}

namespace Core.Interfaces.Events.GameEvents
{
    using Entity;

    public record EntityBarrierChanges(IFightable Entity, float Value) : IGameEvent, IBattleEvent;
}

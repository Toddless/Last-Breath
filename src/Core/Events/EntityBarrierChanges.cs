namespace Core.Events
{
    using Entity;

    public record EntityBarrierChanges(IFightable Entity, float Value) : IGameEvent, IBattleEvent;
}

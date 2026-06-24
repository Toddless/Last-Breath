namespace Core.Interfaces.Events.GameEvents
{
    using Enums;

    public record BattleEndEvent(BattleResults Results) : IGameEvent, IBattleEvent;
}

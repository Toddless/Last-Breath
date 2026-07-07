namespace Core.Events.GameEvents
{
    using Enums;

    public record BattleEndEvent(BattleResults Results) : IGameEvent, IBattleEvent;
}

namespace Core.Events
{
    using Enums;

    public record BattleEndEvent(BattleResults Results) : IGameEvent, IBattleEvent;
}

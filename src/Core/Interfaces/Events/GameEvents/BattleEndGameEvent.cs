namespace Core.Interfaces.Events.GameEvents
{
    public record BattleEndGameEvent() : IGameEvent, IBattleEvent;
}

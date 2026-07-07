namespace Core.Events.GameEvents
{
    public record CancelSelectionEvent(string SelectionId) : IGameEvent, IBattleEvent;
}

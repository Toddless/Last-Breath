namespace Core.Events
{
    public record CancelSelectionEvent(string SelectionId) : IGameEvent, IBattleEvent;
}

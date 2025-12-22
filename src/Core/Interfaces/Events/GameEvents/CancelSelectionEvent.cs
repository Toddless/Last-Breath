namespace Core.Interfaces.Events.GameEvents
{
    public record CancelSelectionEvent(string SelectionId) : IGameEvent, IBattleEvent;
}

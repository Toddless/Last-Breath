namespace Core.Events
{
    /// <summary>Player confirmed the current selection (second press on the ability); commits Few/auto modes.</summary>
    public record ConfirmSelectionEvent(string SelectionId) : IBattleEvent;
}

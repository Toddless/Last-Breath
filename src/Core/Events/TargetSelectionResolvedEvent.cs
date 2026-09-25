namespace Core.Events
{
    /// <summary>The selection ended (committed or cancelled): the ability button resets its state.</summary>
    public record TargetSelectionResolvedEvent(string SelectionId) : IBattleEvent;
}

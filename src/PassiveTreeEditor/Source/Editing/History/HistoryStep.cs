namespace PassiveTreeEditor.Source.Editing.History
{
    /// <summary>
    /// One step through the history, as the rest of the tool needs to hear about it: what it was, for
    /// the status line, and — when it was a rename — which id became which, so that what was selected
    /// before the step is still selected after it.
    /// </summary>
    public readonly record struct HistoryStep(string Label, IdSwap? Rename);
}

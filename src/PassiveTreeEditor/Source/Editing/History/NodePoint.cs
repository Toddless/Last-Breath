namespace PassiveTreeEditor.Source.Editing.History
{
    /// <summary>Where a node sat when a gesture started. Two floats and no engine type: the rule that
    /// reads these has to be readable outside a running editor.</summary>
    public readonly record struct NodePoint(float X, float Y);
}

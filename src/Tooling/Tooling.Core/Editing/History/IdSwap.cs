namespace Tooling.Editing.History
{
    /// <summary>
    /// A node's id before a step through the history and after it. Only a rename produces one: every
    /// other edit leaves the id alone, and everything addressing the node by id — the selection, the
    /// allocation — stays correct without being told anything.
    /// </summary>
    public readonly record struct IdSwap(string From, string To);
}

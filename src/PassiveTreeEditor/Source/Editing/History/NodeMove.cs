namespace PassiveTreeEditor.Source.Editing.History
{
    using Core.PassiveTree;

    /// <summary>
    /// Where one node was and where it ended up. Both coordinates are kept as the author left them, so
    /// stepping back writes a number rather than reversing an offset that a second drag would have made
    /// wrong. The node is held directly, not by id: a rename between the move and its undo changes
    /// nothing about which node has to go back.
    /// </summary>
    public readonly record struct NodeMove(PassiveNode Node, float FromX, float FromY, float ToX, float ToY);
}

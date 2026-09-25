namespace PassiveTreeEditor.Source.Editing.History
{
    using System.Collections.Generic;
    using Core.PassiveTree;

    /// <summary>
    /// Which nodes a drag actually moved, read from where they started and where they are now.
    /// <para>The comparison is exact rather than approximate: a drag that went out and came back to the
    /// same coordinates changed nothing, and a step in the stack that restores what is already there is
    /// a step the author presses undo for and sees nothing happen. A node deleted between the start of
    /// the gesture and its end is no longer anything to move back.</para>
    /// </summary>
    public static class NodeMoves
    {
        public static List<NodeMove> Since(PassiveTreeDocument document, IReadOnlyDictionary<string, NodePoint> origins)
        {
            List<NodeMove> moves = [];

            foreach (KeyValuePair<string, NodePoint> origin in origins)
            {
                PassiveNode? node = document.Find(origin.Key);
                if (node is null) continue;
                if (node.X == origin.Value.X && node.Y == origin.Value.Y) continue;

                moves.Add(new NodeMove(node, origin.Value.X, origin.Value.Y, node.X, node.Y));
            }

            return moves;
        }
    }
}

namespace PassiveTreeEditor.Source.Editing.History
{
    using System.Collections.Generic;
    using Core.PassiveTree;

    /// <summary>
    /// A set of nodes back at the coordinates they had. One command per gesture, not per node: a drag
    /// that moved twenty nodes is one thing the author did and has to be one thing they can take back.
    /// </summary>
    public sealed class MoveEdit(PassiveTreeDocument document, IReadOnlyList<NodeMove> moves, string label) : IEditCommand
    {
        public string Label => label;

        public void Undo() => Place(original: true);

        public void Redo() => Place(original: false);

        private void Place(bool original)
        {
            foreach (NodeMove move in moves)
            {
                move.Node.X = original ? move.FromX : move.ToX;
                move.Node.Y = original ? move.FromY : move.ToY;
            }

            // Geometry moved without the graph changing, which is the one case the document cannot
            // notice on its own.
            document.Reindex();
        }
    }
}

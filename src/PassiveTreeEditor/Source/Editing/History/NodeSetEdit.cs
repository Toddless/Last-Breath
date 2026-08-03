namespace PassiveTreeEditor.Source.Editing.History
{
    using System.Collections.Generic;
    using Core.PassiveTree;

    /// <summary>
    /// Nodes and the edges they sat in, going away or coming back together. Creation and deletion are
    /// one command with a direction because each is the other's undo. What makes a deletion reversible
    /// is that the node objects are kept rather than described: everything still holding one — a later
    /// command in this stack, the memory of what a context line said as a number — is holding the same
    /// node when it returns, so nothing has to be re-matched by id.
    /// </summary>
    public sealed class NodeSetEdit(
        PassiveTreeDocument document,
        IReadOnlyList<PassiveNode> nodes,
        IReadOnlyList<NodeLink> links,
        bool creates,
        string label) : IEditCommand
    {
        public string Label => label;

        public void Undo()
        {
            if (creates) Detach();
            else Attach();
        }

        public void Redo()
        {
            if (creates) Attach();
            else Detach();
        }

        /// <summary>Nodes before edges: the document refuses an edge while either end is missing.</summary>
        private void Attach()
        {
            foreach (PassiveNode node in nodes) document.AddNode(node);
            foreach (NodeLink link in links) document.Link(link.A, link.B);
        }

        /// <summary>Removing a node takes its edges with it — which is why they are captured before the
        /// deletion and laid back by hand.</summary>
        private void Detach()
        {
            foreach (PassiveNode node in nodes) document.RemoveNode(node.Id);
        }
    }
}

namespace Core.PassiveTree.View
{
    using System;
    using System.Collections.Generic;

    /// <summary>One authored row of a node-visual library, reduced to the ADDRESS it answers to. What the
    /// row looks like is a Godot resource and stays out of here: the choice between two rows is the same
    /// arithmetic whatever the look is made of.</summary>
    public interface IPassiveNodeVisual
    {
        /// <summary>The node the row was authored for, or empty when the row is a class default.</summary>
        string NodeId { get; }

        /// <summary>The class the row is the default for. Read only when <see cref="NodeId"/> is empty —
        /// a row that names a node is about that node whatever class it belongs to.</summary>
        PassiveNodeKind Kind { get; }

        /// <summary>Which of the two layers the row belongs to, asked in ONE place. Blank is a class row
        /// and so is whitespace, which is not an address; a second spelling of this test downstream is
        /// how a row files itself as a class and is then read as a node's.</summary>
        static bool IsNodeRow(IPassiveNodeVisual row) => !string.IsNullOrWhiteSpace(row.NodeId);
    }

    /// <summary>
    /// Which authored look a node wears: its own row if the library names it, otherwise its class's row,
    /// otherwise nothing. Nothing is a legal answer and the usual one — a wheel with no library, or with
    /// a library silent about a node, is drawn exactly the way it was before the channel existed, so the
    /// look degrades to the shipped one rather than to a placeholder.
    /// <para>Built once per library instead of searched per node: the layer that draws the mass asks
    /// about every node of the document on every redraw.</para>
    /// </summary>
    public sealed class PassiveNodeVisualIndex<TVisual> where TVisual : class, IPassiveNodeVisual
    {
        private readonly Dictionary<string, TVisual> _byNode = new(StringComparer.Ordinal);
        private readonly Dictionary<PassiveNodeKind, TVisual> _byKind = [];

        /// <summary>Rows are read in order and a later one replaces an earlier one at the same address, so
        /// a duplicate is settled by the bottom of the list its author is looking at. Empty slots are
        /// skipped: the editor puts one in the array the moment a row is added.</summary>
        public PassiveNodeVisualIndex(IEnumerable<TVisual?> rows)
        {
            foreach (TVisual? row in rows)
            {
                if (row == null) continue;

                if (IPassiveNodeVisual.IsNodeRow(row)) _byNode[row.NodeId] = row;
                else _byKind[row.Kind] = row;
            }
        }

        /// <summary>The node's own row over its class's row, and null when the library says nothing about
        /// either.</summary>
        public TVisual? For(string nodeId, PassiveNodeKind kind) =>
            _byNode.TryGetValue(nodeId, out TVisual? own) ? own : _byKind.GetValueOrDefault(kind);

        public TVisual? For(PassiveNode node) => For(node.Id, node.Kind);
    }
}

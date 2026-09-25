namespace Core.PassiveTree.Editing
{
    using System;
    using System.Collections.Generic;
    using PropertyRows = System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, float>>;

    /// <summary>
    /// One node's share of a group edit: what its field held and what the edit gives it. The old value is
    /// each node's own — a group of five that shared a title still had five titles a moment before, and a
    /// step back that wrote one of them into all of them would be an undo inventing state.
    /// </summary>
    public sealed class NodeValueChange<T>(PassiveNode node, T from, T to)
    {
        public PassiveNode Node { get; } = node;

        /// <summary>What this node held before the edit — never rewritten, however long the run of
        /// keystrokes on top of it goes on.</summary>
        public T From { get; } = from;

        /// <summary>What the edit leaves it at. Moves with the run: typing into a field is one step, and
        /// the step lands wherever the typing stopped.</summary>
        public T To { get; set; } = to;
    }

    /// <summary>
    /// A change made to several nodes at once, worked out before anything is written. The plan is the
    /// whole point: it is what an undo stack files as one step, and it is where "this node already said
    /// that" is decided — a node the edit does not actually change has no business being in a step that
    /// claims to have changed it.
    /// <para>Plain C# beside the tree rather than inside a panel: a group edit is arithmetic on nodes, and
    /// the two failures worth guarding — a batch that only reaches the first node, and a merged run that
    /// forgets what the nodes started from — are invisible on screen until the file is already wrong.</para>
    /// </summary>
    public static class NodeBatch
    {
        /// <summary>
        /// Which nodes one value would actually change, and what each of them holds now. Nodes already at
        /// the value are left out rather than recorded as no-ops, so an undo of the step puts back only
        /// what the step moved.
        /// </summary>
        public static List<NodeValueChange<T>> Plan<T>(IReadOnlyList<PassiveNode> nodes, Func<PassiveNode, T> read,
            T value, IEqualityComparer<T>? comparer = null)
        {
            IEqualityComparer<T> equality = comparer ?? EqualityComparer<T>.Default;
            List<NodeValueChange<T>> planned = [];

            foreach (PassiveNode node in nodes)
            {
                T current = read(node);
                if (equality.Equals(current, value)) continue;

                planned.Add(new NodeValueChange<T>(node, current, value));
            }

            return planned;
        }

        /// <summary>
        /// One named number written across the group. Each node keeps its own record — its order, its other
        /// keys — and only the one key is rewritten: the rows are handed back whole because a dictionary
        /// gives a fresh key the slot a removed one left behind, and the group must not reorder anybody's
        /// file. A node that does not carry the key is not given it here; the row exists because every node
        /// already writes it.
        /// </summary>
        public static List<NodeValueChange<PropertyRows>> PlanPropertyValue(IReadOnlyList<PassiveNode> nodes,
            string key, float value)
        {
            List<NodeValueChange<PropertyRows>> planned = [];

            foreach (PassiveNode node in nodes)
            {
                if (!node.Properties.TryGetValue(key, out float current) || current == value) continue;

                planned.Add(new NodeValueChange<PropertyRows>(node, node.PropertyRows(), Rewritten(node, key, key, value)));
            }

            return planned;
        }

        /// <summary>
        /// One named number renamed across the group, each node keeping its own value under the new name and
        /// the place in its record the old name held. All or nothing: a node that already writes the new key
        /// would collapse two rows into one number, and which of them survived would be a coin toss — so the
        /// whole batch is refused and the caller puts its dropdown back.
        /// </summary>
        public static List<NodeValueChange<PropertyRows>> PlanPropertyKey(IReadOnlyList<PassiveNode> nodes,
            string fromKey, string toKey)
        {
            List<NodeValueChange<PropertyRows>> planned = [];
            if (string.Equals(fromKey, toKey, StringComparison.Ordinal)) return planned;

            foreach (PassiveNode node in nodes)
            {
                if (!node.Properties.TryGetValue(fromKey, out float value)) continue;
                if (node.Properties.ContainsKey(toKey)) return [];

                planned.Add(new NodeValueChange<PropertyRows>(node, node.PropertyRows(), Rewritten(node, fromKey, toKey, value)));
            }

            return planned;
        }

        /// <summary>Writes the plan, forwards or back. Every node is written from its own entry, so the
        /// order the entries stand in cannot change what the tree ends up saying.</summary>
        public static void Apply<T>(IReadOnlyList<NodeValueChange<T>> changes, Action<PassiveNode, T> write, bool forward)
        {
            foreach (NodeValueChange<T> change in changes) write(change.Node, forward ? change.To : change.From);
        }

        /// <summary>
        /// A newer plan folded into the one still taking keystrokes. Each node keeps the value it had before
        /// the run began and takes the newer destination; a node the earlier plan left out — it already said
        /// what was being typed, until the next letter — joins with the state it held when it was first
        /// moved. That is what makes a group of five typed into one step back rather than one per letter.
        /// </summary>
        public static void Absorb<T>(List<NodeValueChange<T>> into, IReadOnlyList<NodeValueChange<T>> newer)
        {
            foreach (NodeValueChange<T> change in newer)
            {
                NodeValueChange<T>? standing = into.Find(entry => ReferenceEquals(entry.Node, change.Node));

                if (standing is null) into.Add(change);
                else standing.To = change.To;
            }
        }

        /// <summary>The node's record with one key rewritten in place — renamed, revalued, or both. Every
        /// other row keeps its name, its number and its position.</summary>
        private static PropertyRows Rewritten(PassiveNode node, string fromKey, string toKey, float value)
        {
            PropertyRows rows = [];

            foreach (KeyValuePair<string, float> row in node.Properties)
                rows.Add(string.Equals(row.Key, fromKey, StringComparison.Ordinal)
                    ? new KeyValuePair<string, float>(toKey, value)
                    : row);

            return rows;
        }
    }
}

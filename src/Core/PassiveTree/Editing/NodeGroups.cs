namespace Core.PassiveTree.Editing
{
    using System;
    using System.Collections.Generic;
    using Battle.Skills;

    /// <summary>
    /// One field read across several nodes: the value they all hold, or the word that they do not hold one.
    /// <para>The distinction is the whole of group editing. A field the nodes disagree on has no value to
    /// show, and a panel that showed one anyway would offer the author the first node's number as if it
    /// were everybody's — he saves, and four nodes quietly took a fifth one's balance.</para>
    /// </summary>
    public readonly record struct MergedValue<T>(T Value, bool Mixed);

    /// <summary>One named number of a group: the key every node writes it under, and its value where they
    /// all write the same one.</summary>
    public readonly record struct MergedProperty(string Key, float Value, bool Mixed);

    /// <summary>
    /// Whether several selected nodes are the same kind of thing, and what their fields say when read
    /// together. Both questions belong outside the panel: what counts as "the same node again" decides
    /// which controls the author is offered, and a rule that lives in a widget is a rule nothing can test.
    /// </summary>
    public static class NodeGroups
    {
        /// <summary>Family of every node whose passive is built from its own stat lines. They are one
        /// family however their ids read: the fields are the content, and two stat nodes carrying the same
        /// lines are edited together whatever each one is called.</summary>
        public const string StatFamily = "stat";

        /// <summary>Family of a node that grants nothing.</summary>
        public const string NoPassive = "";

        /// <summary>What a node's passive is, for the purpose of being edited beside another one: the stat
        /// family as a whole, a named passive by its own id, or nothing at all. A named passive is its own
        /// family because its fields are what its factory reads — two different factories read two
        /// different sets of names, and a shared row would write a number nobody asked for.</summary>
        public static string PassiveFamily(string? passiveId)
        {
            if (string.IsNullOrWhiteSpace(passiveId)) return NoPassive;

            return StatPassiveGrammar.Owns(passiveId) ? StatFamily : passiveId;
        }

        /// <summary>Whether two nodes carry the same shape of content: the same class, the same passive
        /// family, and the same set of named numbers. The keys are compared as a set rather than as a list
        /// — the order of a record is each node's own, and two nodes writing the same lines in a different
        /// order are still the same node twice.</summary>
        public static bool SameShape(PassiveNode first, PassiveNode second)
        {
            if (first.Kind != second.Kind) return false;
            if (!string.Equals(PassiveFamily(first.PassiveId), PassiveFamily(second.PassiveId), StringComparison.Ordinal))
                return false;

            if (first.Properties.Count != second.Properties.Count) return false;

            foreach (KeyValuePair<string, float> property in first.Properties)
                if (!second.Properties.ContainsKey(property.Key))
                    return false;

            return true;
        }

        /// <summary>Whether every node in the selection is the same shape as the first. A selection of one
        /// — or of none — is uniform: there is nothing in it to disagree.</summary>
        public static bool Uniform(IReadOnlyList<PassiveNode> nodes)
        {
            for (int index = 1; index < nodes.Count; index++)
                if (!SameShape(nodes[0], nodes[index]))
                    return false;

            return true;
        }

        /// <summary>
        /// One field of every node read together: the value they share, or mixed. An empty selection has
        /// nothing to disagree about and answers with the default value unmixed — a panel showing no nodes
        /// shows no fields either, so nothing is ever written from it.
        /// </summary>
        public static MergedValue<T> Merge<T>(IReadOnlyList<PassiveNode> nodes, Func<PassiveNode, T> read,
            IEqualityComparer<T>? comparer = null)
        {
            if (nodes.Count == 0) return new MergedValue<T>(default!, Mixed: false);

            IEqualityComparer<T> equality = comparer ?? EqualityComparer<T>.Default;
            T first = read(nodes[0]);

            for (int index = 1; index < nodes.Count; index++)
                if (!equality.Equals(first, read(nodes[index])))
                    return new MergedValue<T>(default!, Mixed: true);

            return new MergedValue<T>(first, Mixed: false);
        }

        /// <summary>
        /// The named numbers of a uniform group, one row per key, in the order the first node wrote them —
        /// the same order the panel shows a single node's record in. A key some node in the selection does
        /// not carry is left out: a row standing for a number that is not there would write it into every
        /// node the moment it was touched.
        /// </summary>
        public static List<MergedProperty> MergeProperties(IReadOnlyList<PassiveNode> nodes)
        {
            List<MergedProperty> merged = [];
            if (nodes.Count == 0) return merged;

            foreach (KeyValuePair<string, float> property in nodes[0].Properties)
            {
                if (!EveryNodeHolds(nodes, property.Key)) continue;

                merged.Add(MergeProperty(nodes, property.Key, property.Value));
            }

            return merged;
        }

        private static bool EveryNodeHolds(IReadOnlyList<PassiveNode> nodes, string key)
        {
            for (int index = 1; index < nodes.Count; index++)
                if (!nodes[index].Properties.ContainsKey(key))
                    return false;

            return true;
        }

        private static MergedProperty MergeProperty(IReadOnlyList<PassiveNode> nodes, string key, float first)
        {
            for (int index = 1; index < nodes.Count; index++)
                if (nodes[index].Properties[key] != first)
                    return new MergedProperty(key, 0f, Mixed: true);

            return new MergedProperty(key, first, Mixed: false);
        }
    }
}

namespace Core.PassiveTree.View
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Which nodes get a scene of their own and which are drawn as part of the mass, decided once. Split
    /// by how often a node changes, not by its class: a node carries a scene when its class is one the
    /// look gives a scene to, OR when it is taken (a taken node animates, pulses, wears an augment mark,
    /// and there are never more of those than the point budget allows). Everything else is a circle in a
    /// single draw call.
    /// <para>Written here rather than in the two layers that need the answer, since they are exact
    /// complements by construction (one draws what the set holds, the other what it doesn't) — a second
    /// reading of the rule would be the one way a node draws twice or vanishes.</para>
    /// </summary>
    public static class NodeCarriers
    {
        /// <summary>Fills <paramref name="carriers"/> with the ids that get a scene, reading the taken set
        /// and the document once each so the caller can't answer the same question two different ways.</summary>
        /// <param name="hasView">Whether the class is drawn as a scene at all — a flag of the look, so
        /// moving a class between the two layers is a resource change, not a code change.</param>
        public static void Collect(
            PassiveTreeDocument document,
            IReadOnlyCollection<string> taken,
            Func<PassiveNodeKind, bool> hasView,
            ISet<string> carriers)
        {
            carriers.Clear();

            foreach (PassiveNode node in document.Nodes)
                if (hasView(node.Kind))
                    carriers.Add(node.Id);

            // Ids the document no longer knows are skipped: a scene would be built for a node with no
            // coordinates to stand at.
            foreach (string id in taken)
                if (document.Contains(id))
                    carriers.Add(id);
        }
    }
}

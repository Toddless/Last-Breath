namespace Core.PassiveTree.View
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Which nodes get a scene of their own and which are drawn as part of the mass, decided once.
    /// <para>The split is by how often a node changes, not by what class it is: a node carries a scene
    /// when its class is one the look gives a scene to, OR when it is taken — because a taken node is
    /// what animates, pulses and wears an augment mark, and there are never more taken nodes than the
    /// point budget allows. Everything else is a circle in a single draw call.</para>
    /// <para>Written here rather than in the two layers that need the answer: they are exact
    /// complements by construction — one draws what the set holds, the other draws what it does not —
    /// so a second reading of the rule would be the one way a node draws twice or vanishes.</para>
    /// </summary>
    public static class NodeCarriers
    {
        /// <summary>
        /// Fills <paramref name="carriers"/> with the ids that get a scene. The taken set is read once
        /// and the document once, so the caller cannot end up answering the same question from two
        /// different readings.
        /// </summary>
        /// <param name="hasView">Whether the class is drawn as a scene at all — a flag of the look, so
        /// moving a class between the two layers is a change to a resource and not to code.</param>
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

            // Ids the document no longer knows are skipped rather than carried: a scene would be built
            // for a node with no coordinates to stand at.
            foreach (string id in taken)
                if (document.Contains(id))
                    carriers.Add(id);
        }
    }
}

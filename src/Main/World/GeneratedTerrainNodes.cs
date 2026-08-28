namespace LastBreath.World
{
    using Godot;

    /// <summary>
    /// Marks and sweeps the nodes a terrain builds for itself — display layers, their components and scattered
    /// props. A generated node is never given an <see cref="Node.Owner"/>, which is what keeps it out of the
    /// saved scene; the mark is what lets the next build recognise it, so re-opening a scene or reloading the
    /// assembly rebuilds the preview instead of doubling it.
    /// </summary>
    public static class GeneratedTerrainNodes
    {
        private const string GeneratedMeta = "terrain_generated";

        /// <summary>Stamps a node built from code. Call it before the node enters the tree.</summary>
        public static void Mark(Node node) => node.SetMeta(GeneratedMeta, true);

        /// <summary>Whether a node came out of a build rather than out of the scene.</summary>
        public static bool IsGenerated(Node node) => node.HasMeta(GeneratedMeta);

        /// <summary>Drops every generated child of a node; hand-authored children are left alone.</summary>
        public static void Purge(Node parent)
        {
            foreach (Node child in parent.GetChildren())
            {
                if (!IsGenerated(child)) continue;

                // Out of the tree before it is queued: the rebuild that follows reuses these names, and a name
                // still held by a node awaiting deletion would push the fresh one to a numbered variant.
                parent.RemoveChild(child);
                child.QueueFree();
            }
        }
    }
}

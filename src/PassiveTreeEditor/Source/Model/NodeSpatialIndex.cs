namespace PassiveTreeEditor.Source.Model
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Uniform grid over node positions. The canvas draws and hit-tests through it so both costs
    /// scale with what is on screen rather than with tree size — the reason the editor stays
    /// responsive when the tree grows past its planned ~245 nodes.
    /// </summary>
    public sealed class NodeSpatialIndex
    {
        private const float CellSize = 160f;

        private readonly Dictionary<long, List<PassiveNode>> _cells = new();

        private static long CellKey(int cellX, int cellY) => ((long)cellX << 32) | (uint)cellY;

        private static int CellOf(float value) => (int)MathF.Floor(value / CellSize);

        public void Rebuild(IReadOnlyList<PassiveNode> nodes)
        {
            foreach (List<PassiveNode> bucket in _cells.Values) bucket.Clear();

            foreach (PassiveNode node in nodes)
            {
                long key = CellKey(CellOf(node.X), CellOf(node.Y));
                if (!_cells.TryGetValue(key, out List<PassiveNode>? bucket))
                {
                    bucket = [];
                    _cells[key] = bucket;
                }

                bucket.Add(node);
            }
        }

        /// <summary>Appends every node whose cell overlaps the world-space box. Cheap superset — the
        /// caller still tests each candidate, which is what makes the cell size a non-critical choice.</summary>
        public void Query(float minX, float minY, float maxX, float maxY, List<PassiveNode> results)
        {
            int firstX = CellOf(minX);
            int lastX = CellOf(maxX);
            int firstY = CellOf(minY);
            int lastY = CellOf(maxY);

            for (int cellX = firstX; cellX <= lastX; cellX++)
                for (int cellY = firstY; cellY <= lastY; cellY++)
                    if (_cells.TryGetValue(CellKey(cellX, cellY), out List<PassiveNode>? bucket))
                        results.AddRange(bucket);
        }
    }
}

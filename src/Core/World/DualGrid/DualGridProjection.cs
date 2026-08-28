namespace Core.World.DualGrid
{
    using System.Collections.Generic;

    /// <summary>
    /// The world terrain data (one bit per cell) together with the display tiles it projects onto. Every edit
    /// answers with exactly the display cells whose mask can have changed, so a repaint never rescans the map.
    /// </summary>
    public sealed class DualGridProjection
    {
        private static readonly IReadOnlyCollection<GridCoordinate> Unchanged = [];

        private readonly HashSet<GridCoordinate> _present = [];

        /// <summary>Mask of a display cell: a bit for every corner whose world cell holds the terrain.</summary>
        public TerrainCorner MaskAt(GridCoordinate display)
        {
            TerrainCorner mask = TerrainCorner.None;
            foreach (TerrainCorner corner in DualGridLayout.Corners)
            {
                if (_present.Contains(DualGridLayout.WorldCellOf(display, corner))) mask |= corner;
            }

            return mask;
        }

        /// <summary>Every display cell that shows terrain — the repaint set of a freshly cleared display.</summary>
        public IReadOnlyCollection<GridCoordinate> DisplayCells()
        {
            HashSet<GridCoordinate> cells = [];
            foreach (GridCoordinate world in _present) DualGridLayout.CollectDisplayCells(world, cells);

            return cells;
        }

        /// <summary>Writes one world cell. Returns the display cells to repaint, empty when nothing changed.</summary>
        public IReadOnlyCollection<GridCoordinate> SetCell(GridCoordinate world, bool present)
        {
            bool changed = present ? _present.Add(world) : _present.Remove(world);
            if (!changed) return Unchanged;

            HashSet<GridCoordinate> dirty = [];
            DualGridLayout.CollectDisplayCells(world, dirty);

            return dirty;
        }

        /// <summary>
        /// Replaces the world data wholesale and returns the display cells to repaint — the delta against the
        /// data held so far, not the whole map.
        /// </summary>
        public IReadOnlyCollection<GridCoordinate> Reconcile(IEnumerable<GridCoordinate> presentCells)
        {
            HashSet<GridCoordinate> incoming = [.. presentCells];
            HashSet<GridCoordinate> dirty = [];

            foreach (GridCoordinate cell in incoming)
            {
                if (!_present.Contains(cell)) DualGridLayout.CollectDisplayCells(cell, dirty);
            }

            foreach (GridCoordinate cell in _present)
            {
                if (!incoming.Contains(cell)) DualGridLayout.CollectDisplayCells(cell, dirty);
            }

            _present.Clear();
            _present.UnionWith(incoming);

            return dirty;
        }
    }
}

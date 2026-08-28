namespace Core.World.DualGrid
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Address arithmetic of the half-cell shift. The display grid sits half a cell up and left of the world
    /// grid, so display cell (x, y) covers the corners of world cells (x-1, y-1), (x, y-1), (x-1, y), (x, y)
    /// and its centre lands on the point those four share. Both directions are plain offsets — no division,
    /// so negative addresses behave exactly like positive ones.
    /// </summary>
    public static class DualGridLayout
    {
        /// <summary>Number of display tiles a single world cell takes part in — one per corner role.</summary>
        public const int DisplayCellsPerWorldCell = 4;

        /// <summary>Corners in mask bit order; iterating this keeps the order in a single place.</summary>
        public static readonly TerrainCorner[] Corners =
        [
            TerrainCorner.TopLeft,
            TerrainCorner.TopRight,
            TerrainCorner.BottomLeft,
            TerrainCorner.BottomRight
        ];

        private static GridCoordinate OffsetOf(TerrainCorner corner) => corner switch
        {
            TerrainCorner.TopLeft => new GridCoordinate(-1, -1),
            TerrainCorner.TopRight => new GridCoordinate(0, -1),
            TerrainCorner.BottomLeft => new GridCoordinate(-1, 0),
            TerrainCorner.BottomRight => new GridCoordinate(0, 0),
            _ => throw new ArgumentOutOfRangeException(nameof(corner), corner, "Expected a single corner bit.")
        };

        /// <summary>World cell whose corner the display cell covers in the given role.</summary>
        public static GridCoordinate WorldCellOf(GridCoordinate display, TerrainCorner corner) =>
            display + OffsetOf(corner);

        /// <summary>Display cell covering the world cell in the given role — inverse of <see cref="WorldCellOf"/>.</summary>
        public static GridCoordinate DisplayCellOf(GridCoordinate world, TerrainCorner corner) =>
            world - OffsetOf(corner);

        /// <summary>Appends the four display cells whose mask a write to this world cell can change.</summary>
        public static void CollectDisplayCells(GridCoordinate world, ICollection<GridCoordinate> destination)
        {
            foreach (TerrainCorner corner in Corners) destination.Add(DisplayCellOf(world, corner));
        }

        /// <summary>
        /// Pixel offset of the display layer against the world layer: half a cell up and left, which puts the
        /// centre of display cell (0, 0) on the shared corner of the four world cells it covers.
        /// </summary>
        public static float DisplayPixelOffset(int tileSize) => -tileSize / 2f;
    }
}

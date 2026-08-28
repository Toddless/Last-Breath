namespace Core.World.DualGrid
{
    using System;

    /// <summary>
    /// Atlas layout shared by every transition sheet: 4x4 tiles, the mask value is the tile index row-major.
    /// A new terrain pair is therefore a new PNG with the same layout and no code change.
    /// </summary>
    public static class DualGridAtlas
    {
        public const int Columns = 4;
        public const int Rows = 4;
        public const int MaskCount = Columns * Rows;

        /// <summary>Atlas cell holding the tile drawn for the mask.</summary>
        public static GridCoordinate CoordinateOf(TerrainCorner mask)
        {
            int index = (int)mask;
            if (index < 0 || index >= MaskCount)
                throw new ArgumentOutOfRangeException(nameof(mask), mask, "Mask is outside the four corner bits.");

            return new GridCoordinate(index % Columns, index / Columns);
        }
    }
}

namespace Core.World.DualGrid
{
    using System;

    public enum TransitionAtlasLayout
    {
        RowMajor,
        Grouped
    }

    /// <summary>Maps corner masks to cells in a 4x4 transition sheet.</summary>
    public static class DualGridAtlas
    {
        public const int Columns = 4;
        public const int Rows = 4;
        public const int MaskCount = Columns * Rows;

        // Cell indices by mask, for rows 1,2,8,4 / 3,10,9,6 / 12,5,7,11 / 13,14,0,15.
        private static readonly int[] s_groupedIndices = [14, 0, 1, 4, 3, 9, 7, 10, 2, 6, 5, 11, 8, 12, 13, 15];

        /// <summary>Atlas cell in the original binary-order sheet.</summary>
        public static GridCoordinate CoordinateOf(TerrainCorner mask) => CoordinateOf(mask, TransitionAtlasLayout.RowMajor);

        /// <summary>Atlas cell holding the tile drawn for the mask in the selected layout.</summary>
        public static GridCoordinate CoordinateOf(TerrainCorner mask, TransitionAtlasLayout layout)
        {
            int index = (int)mask;
            if (index < 0 || index >= MaskCount)
                throw new ArgumentOutOfRangeException(nameof(mask), mask, "Mask is outside the four corner bits.");

            index = layout switch
            {
                TransitionAtlasLayout.RowMajor => index,
                TransitionAtlasLayout.Grouped => s_groupedIndices[index],
                _ => throw new ArgumentOutOfRangeException(nameof(layout), layout, "Unknown transition atlas layout.")
            };

            return new GridCoordinate(index % Columns, index / Columns);
        }
    }
}

namespace Core.World.DualGrid
{
    using System;

    /// <summary>
    /// The four world cells whose corners a single display tile covers, one bit each. A combination of the
    /// bits is that tile's mask; the bit order TL=1, TR=2, BL=4, BR=8 is the atlas order and the only
    /// place it is fixed.
    /// </summary>
    [Flags]
    public enum TerrainCorner
    {
        None = 0,
        TopLeft = 1 << 0,
        TopRight = 1 << 1,
        BottomLeft = 1 << 2,
        BottomRight = 1 << 3,
        All = TopLeft | TopRight | BottomLeft | BottomRight
    }
}

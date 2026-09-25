namespace Core.World.DualGrid
{
    /// <summary>
    /// One prop grown on a world cell. Offsets are pixels from the cell's top-left corner and point at the
    /// prop's root — the point the renderer anchors and sorts by — not at its centre.
    /// </summary>
    /// <param name="OffsetX">Root offset from the cell's left edge, inside the edge margin.</param>
    /// <param name="OffsetY">Root offset from the cell's top edge, inside the edge margin.</param>
    /// <param name="Mirrored">Whether the copy is flipped horizontally.</param>
    /// <param name="Scale">Size multiplier around the root.</param>
    /// <param name="TintShift">Brightness shift, negative darkens and positive brightens.</param>
    public readonly record struct PropPlacement(
        float OffsetX,
        float OffsetY,
        bool Mirrored,
        float Scale,
        float TintShift);
}

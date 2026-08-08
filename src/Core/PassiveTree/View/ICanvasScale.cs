namespace Core.PassiveTree.View
{
    /// <summary>
    /// The two readings of scale a drawing needs, and deliberately nothing else.
    /// <para>A SIZE travels through the zoom alone — a node is the same number of pixels across at every
    /// spread, which is the whole meaning of <see cref="CanvasTransform.Spread"/>. A LENGTH measured on
    /// screen becomes a length inside the frame through <see cref="DocumentLength"/>, which divides by
    /// the position scale, because the frame multiplies by that.</para>
    /// <para>It exists so a bare zoom cannot be handed where the frame's own scale is meant: every
    /// "screen pixels to frame units" conversion in the wheel used to be written as a division by the
    /// zoom, and every one of them was right only while the spread stayed at one.</para>
    /// </summary>
    public interface ICanvasScale
    {
        float Zoom { get; }

        /// <summary>A length measured on screen, in the units the frame will scale back up.</summary>
        float DocumentLength(float screenLength);
    }

    /// <summary>
    /// Where the drawing frame sits and what it multiplies by — the whole of what a canvas writes onto
    /// its engine node. Mechanical on purpose: a node drawn at <c>X + node.X * Scale</c> is a node the
    /// picker finds at that pixel, and the two cannot drift while both read this.
    /// </summary>
    public readonly record struct CanvasFrame(float X, float Y, float Scale);
}

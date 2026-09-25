namespace Core.PassiveTree.View
{
    /// <summary>
    /// The two readings of scale a drawing needs, deliberately nothing else. A SIZE travels through zoom
    /// alone — a node is the same pixel size at every spread, the whole meaning of
    /// <see cref="CanvasTransform.Spread"/>. A LENGTH measured on screen becomes a frame length through
    /// <see cref="DocumentLength"/>, dividing by the position scale, since the frame multiplies by that.
    /// Exists so a bare zoom can't be handed where the frame's own scale is meant — every "screen pixels
    /// to frame units" conversion used to be a division by zoom alone, correct only while spread stayed 1.
    /// </summary>
    public interface ICanvasScale
    {
        float Zoom { get; }

        /// <summary>A length measured on screen, in the units the frame will scale back up.</summary>
        float DocumentLength(float screenLength);
    }

    /// <summary>Where the drawing frame sits and what it multiplies by — the whole of what a canvas
    /// writes onto its engine node. Mechanical on purpose: a node drawn at <c>X + node.X * Scale</c> is a
    /// node the picker finds at that pixel, and the two cannot drift while both read this.</summary>
    public readonly record struct CanvasFrame(float X, float Y, float Scale);
}

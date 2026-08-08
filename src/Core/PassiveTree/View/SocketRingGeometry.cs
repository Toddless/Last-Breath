namespace Core.PassiveTree.View
{
    using System;

    /// <summary>Where one pip of a socket ring sits and how big it is: the centre in document units, and
    /// the radius in both spaces, so whoever draws it and whoever picks it read the same two numbers.</summary>
    public readonly record struct SocketPipPlacement(float X, float Y, float DocumentRadius, float ScreenRadius);

    /// <summary>
    /// The ring of augment slots drawn around an ability node: where each place of it sits and how big
    /// it is.
    ///
    /// <para>Everything is a multiple of what the NODE measures on screen
    /// (<see cref="NodeGeometry.ScreenRadius"/>) and never a gap in pixels — the node's size has a floor
    /// under it and a gap does not, so a fixed offset outlives the floor and the ring ends up several
    /// times the dot it belongs to. The same rule <see cref="NodeGeometry.ScreenRingRadius"/> is written
    /// under, applied to a mark that also has to be aimed at.</para>
    ///
    /// <para>The ring is an ARC and not a circle, centred on the direction away from the middle of the
    /// wheel: branches leave a node towards its neighbours, and pips spread over the whole circle would
    /// sit on top of them.</para>
    /// </summary>
    /// <param name="nodes">The one table of node sizes. Borrowed rather than copied: a ring measured off
    /// a second reading of a node's size would drift from the node it is about.</param>
    /// <param name="ringRadiusScale">How far out the pip centres sit, as a multiple of the node's own
    /// screen radius.</param>
    /// <param name="pipRadiusScale">How big a pip is, as a multiple of the node's own screen radius.</param>
    /// <param name="arcDegrees">How wide the fan of pips opens.</param>
    /// <param name="minPipScreenRadius">The floor under a pip, so it stays visible and aimable.</param>
    /// <param name="minNodeScreenRadius">How small the NODE may get before its ring stops being drawn at
    /// all. A mark on a dot a pixel and a half across has stopped being that dot's mark.</param>
    public sealed class SocketRingGeometry(
        NodeGeometry nodes,
        float ringRadiusScale,
        float pipRadiusScale,
        float arcDegrees,
        float minPipScreenRadius,
        float minNodeScreenRadius)
    {
        /// <summary>Whether the ring is worth drawing at this zoom at all. Asked by the drawing and by
        /// the pick alike: a pip nobody can see must not answer a click either.</summary>
        public bool Draws(PassiveNodeKind kind, float zoom) => nodes.ScreenRadius(kind, zoom) >= minNodeScreenRadius;

        /// <summary>
        /// Where the <paramref name="index"/>-th of <paramref name="count"/> pips of one node's ring sits.
        /// A lone pip sits on the outward direction itself; several fan out evenly across the arc.
        /// </summary>
        public SocketPipPlacement Place(float nodeX, float nodeY, PassiveNodeKind kind, ICanvasScale scale, int index, int count)
        {
            float radians = Outward(nodeX, nodeY) + Offset(index, count);
            float ring = nodes.DocumentRingRadius(kind, scale, ringRadiusScale);
            float screenPip = ScreenPipRadius(kind, scale.Zoom);

            return new SocketPipPlacement(
                nodeX + MathF.Cos(radians) * ring,
                nodeY + MathF.Sin(radians) * ring,
                scale.DocumentLength(screenPip),
                screenPip);
        }

        /// <summary>The direction away from the middle of the wheel. A node standing exactly at the
        /// middle has no outward direction, so it takes the one the fan opens from by default.</summary>
        /// <summary>How big a pip is on screen: a share of the node's own size, never below the floor
        /// that keeps it visible and aimable.</summary>
        private float ScreenPipRadius(PassiveNodeKind kind, float zoom) =>
            MathF.Max(nodes.ScreenRadius(kind, zoom) * pipRadiusScale, minPipScreenRadius);

        private static float Outward(float nodeX, float nodeY) =>
            nodeX == 0f && nodeY == 0f ? 0f : MathF.Atan2(nodeY, nodeX);

        private float Offset(int index, int count)
        {
            if (count <= 1) return 0f;

            float arc = arcDegrees * MathF.PI / 180f;
            return -arc * 0.5f + arc * index / (count - 1);
        }
    }
}

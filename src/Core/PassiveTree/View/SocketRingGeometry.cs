namespace Core.PassiveTree.View
{
    using System;

    /// <summary>Where one pip of a socket ring sits and how big it is: centre in document units, radius in
    /// both spaces, so drawing and picking read the same two numbers.</summary>
    public readonly record struct SocketPipPlacement(float X, float Y, float DocumentRadius, float ScreenRadius);

    /// <summary>
    /// The ring of augment slots drawn around an ability node: where each place sits and how big it is.
    /// Everything is a multiple of the NODE's own screen radius (<see cref="NodeGeometry.ScreenRadius"/>),
    /// never a fixed pixel gap — the node's floor would otherwise leave a fixed offset several times the
    /// dot's size at low zoom (same rule as <see cref="NodeGeometry.ScreenRingRadius"/>).
    /// <para>The ring is an ARC, not a circle, centred away from the wheel's middle: branches leave a node
    /// towards its neighbours, and pips spread over a full circle would sit on top of them.</para>
    /// </summary>
    /// <param name="nodes">The one table of node sizes, borrowed rather than copied so a ring can't drift
    /// from the node it's about.</param>
    /// <param name="ringRadiusScale">How far out pip centres sit, as a multiple of the node's radius.</param>
    /// <param name="pipRadiusScale">How big a pip is, as a multiple of the node's radius.</param>
    /// <param name="arcDegrees">How wide the fan of pips opens.</param>
    /// <param name="minPipScreenRadius">Floor under a pip so it stays visible and aimable.</param>
    /// <param name="minNodeScreenRadius">How small the NODE may get before its ring stops drawing at all —
    /// a mark on a pixel-and-a-half dot has stopped being that dot's mark.</param>
    public sealed class SocketRingGeometry(
        NodeGeometry nodes,
        float ringRadiusScale,
        float pipRadiusScale,
        float arcDegrees,
        float minPipScreenRadius,
        float minNodeScreenRadius)
    {
        /// <summary>Whether the ring is worth drawing at this zoom — asked by drawing and picking alike,
        /// since an invisible pip must not answer a click either.</summary>
        public bool Draws(PassiveNodeKind kind, float zoom) => nodes.ScreenRadius(kind, zoom) >= minNodeScreenRadius;

        /// <summary>Where the <paramref name="index"/>-th of <paramref name="count"/> pips sits. A lone
        /// pip sits on the outward direction itself; several fan out evenly across the arc.</summary>
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

        /// <summary>How big a pip is on screen: a share of the node's own size, never below the floor.</summary>
        private float ScreenPipRadius(PassiveNodeKind kind, float zoom) =>
            MathF.Max(nodes.ScreenRadius(kind, zoom) * pipRadiusScale, minPipScreenRadius);

        /// <summary>Direction away from the wheel's middle; a node exactly at the middle has none, so it
        /// takes the fan's default direction.</summary>
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

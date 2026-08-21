namespace Core.PassiveTree.View
{
    using System;

    /// <summary>Where one pip of a socket ring sits and how big it is: centre in document units, radius in
    /// both spaces, so drawing and picking read the same two numbers.</summary>
    public readonly record struct SocketPipPlacement(float X, float Y, float DocumentRadius, float ScreenRadius);

    /// <summary>Which way a node's fan of pips opens and how wide. A property of the NODE rather than of
    /// any one pip, so it is read once per ring and handed to every place on it.</summary>
    public readonly record struct SocketRingFan(float CentreRadians, float ArcRadians);

    /// <summary>
    /// The ring of augment slots drawn around an ability node: where each place sits and how big it is.
    /// Sizes are multiples of the NODE's own screen radius and never a fixed pixel gap — the same rule as
    /// <see cref="NodeGeometry.ScreenRingRadius"/>, and for the same reason.
    /// <para>The ring is an ARC laid in the widest gap between the branches leaving the node, squeezed to
    /// fit where the gap is narrow: the tree is radial, so a fan aimed away from the wheel's middle opens
    /// straight onto the lines to the node's own children. Where it is read off — the node's place and its
    /// links — is the document alone, so the fan moves with neither the zoom nor the allocation.</para>
    /// </summary>
    /// <param name="nodes">The one table of node sizes, borrowed rather than copied so a ring can't drift
    /// from the node it's about.</param>
    /// <param name="ringRadiusScale">How far out pip centres sit, as a multiple of the node's radius.</param>
    /// <param name="pipRadiusScale">How big a pip is, as a multiple of the node's radius.</param>
    /// <param name="arcDegrees">How wide the fan opens where the gap has room for all of it.</param>
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
        /// <summary>Two directions this close are one, and two gaps this close are as wide as each other —
        /// otherwise a symmetric node would pick its side by the order the graph lists neighbours in.</summary>
        private const float SameAngleRadians = 1e-3f;

        /// <summary>Daylight kept between a pip and whatever it is fitted clear of: the branch beside it,
        /// and the next pip along.</summary>
        private const float PipClearanceRadians = 0.01f;

        /// <summary>How much of a turn a pip covers on its ring, from the AUTHORED sizes alone. The screen
        /// floors under a pip and a node would otherwise make the fan's width a function of the zoom, and
        /// pips that shuffle as the view moves are worse than pips a little wide.</summary>
        private float AngularPipRadius =>
            MathF.Asin(MathF.Min(pipRadiusScale / MathF.Max(ringRadiusScale, 1f), 1f));

        /// <summary>Whether the ring is worth drawing at this zoom — asked by drawing and picking alike,
        /// since an invisible pip must not answer a click either.</summary>
        public bool Draws(PassiveNodeKind kind, float zoom) => nodes.ScreenRadius(kind, zoom) >= minNodeScreenRadius;

        /// <summary>How the node's fan opens: into the widest gap between its branches, at whatever width
        /// that gap leaves. Read once per ring.</summary>
        public SocketRingFan Fan(PassiveTreeDocument document, PassiveNode node, int count)
        {
            (float centre, float gap) = WidestGap(document, node);

            return new SocketRingFan(centre, ArcWithin(gap, count));
        }

        /// <summary>Where the <paramref name="index"/>-th of <paramref name="count"/> pips sits. A lone pip
        /// sits on the fan's own direction; several fan out evenly across its arc.</summary>
        public SocketPipPlacement Place(
            SocketRingFan fan, PassiveNode node, ICanvasScale scale, int index, int count)
        {
            float radians = fan.CentreRadians + Offset(index, count, fan.ArcRadians);
            float ring = nodes.DocumentRingRadius(node.Kind, scale, ringRadiusScale);
            float screenPip = ScreenPipRadius(node.Kind, scale.Zoom);

            return new SocketPipPlacement(
                node.X + MathF.Cos(radians) * ring,
                node.Y + MathF.Sin(radians) * ring,
                scale.DocumentLength(screenPip),
                screenPip);
        }

        /// <summary>How big a pip is on screen: a share of the node's own size, never below the floor.</summary>
        private float ScreenPipRadius(PassiveNodeKind kind, float zoom) =>
            MathF.Max(nodes.ScreenRadius(kind, zoom) * pipRadiusScale, minPipScreenRadius);

        /// <summary>How wide the fan opens inside a gap of this width: the authored arc where the gap has
        /// room for it and for the pips' own width besides, narrower where it has not. The squeeze stops
        /// where neighbouring pips would begin to touch — pips drawn over each other say less than a pip a
        /// branch clips — so a gap narrower even than that keeps the floor and takes the clipping.</summary>
        private float ArcWithin(float gap, int count)
        {
            if (count <= 1) return 0f;

            float authored = Radians(arcDegrees);
            float pip = AngularPipRadius;
            float floor = MathF.Min(authored, (count - 1) * (2f * pip + PipClearanceRadians));

            return Math.Clamp(gap - 2f * (pip + PipClearanceRadians), floor, authored);
        }

        /// <summary>The widest gap between the node's own branches: which way its middle points, and how
        /// much room it leaves. A node nothing leaves has the whole turn, pointing away from the wheel's
        /// middle — the direction the ring took before there were branches to avoid.</summary>
        private static (float Centre, float Gap) WidestGap(PassiveTreeDocument document, PassiveNode node)
        {
            float outward = Outward(node.X, node.Y);
            float centre = outward;
            float widest = MathF.Tau;
            bool found = false;

            foreach (string neighbourId in document.Neighbours(node.Id))
            {
                if (Direction(node, document.Find(neighbourId)) is not { } branch) continue;

                float gap = GapAfter(document, node, branch);
                if (found && gap < widest - SameAngleRadians) continue;

                float middle = branch + gap * 0.5f;
                if (found && gap <= widest + SameAngleRadians && !Nearer(middle, centre, outward)) continue;

                found = true;
                centre = middle;
                widest = gap;
            }

            return (centre, widest);
        }

        /// <summary>How far the next branch is, turning one way from <paramref name="branch"/>. A whole
        /// turn when the node has no second direction to run into, which is the gap a leaf's ring opens
        /// into.</summary>
        private static float GapAfter(PassiveTreeDocument document, PassiveNode node, float branch)
        {
            float gap = MathF.Tau;

            foreach (string neighbourId in document.Neighbours(node.Id))
            {
                if (Direction(node, document.Find(neighbourId)) is not { } other) continue;

                float step = Ahead(other - branch);
                if (step > SameAngleRadians && step < gap) gap = step;
            }

            return gap;
        }

        /// <summary>Which of two equally wide gaps wins: the one opening nearer the way away from the
        /// wheel's middle, then the angle itself — never the order the graph hands neighbours over in.</summary>
        private static bool Nearer(float candidate, float centre, float outward)
        {
            float closer = Apart(candidate, outward) - Apart(centre, outward);

            return MathF.Abs(closer) > SameAngleRadians ? closer < 0f : Ahead(candidate) < Ahead(centre);
        }

        /// <summary>Which way the branch to <paramref name="neighbour"/> leaves, or null when there is no
        /// direction to read off it — a link into a node the document no longer holds, or one sitting in
        /// the very same place.</summary>
        private static float? Direction(PassiveNode node, PassiveNode? neighbour)
        {
            if (neighbour == null) return null;

            float deltaX = neighbour.X - node.X;
            float deltaY = neighbour.Y - node.Y;

            return deltaX == 0f && deltaY == 0f ? null : MathF.Atan2(deltaY, deltaX);
        }

        /// <summary>How far apart two directions are, the short way round: none to half a turn.</summary>
        private static float Apart(float first, float second)
        {
            float delta = Ahead(first - second);

            return delta > MathF.PI ? MathF.Tau - delta : delta;
        }

        /// <summary>The same angle read as a turn in one direction: none to a whole turn.</summary>
        private static float Ahead(float radians)
        {
            float ahead = radians % MathF.Tau;

            return ahead < 0f ? ahead + MathF.Tau : ahead;
        }

        /// <summary>Direction away from the wheel's middle; a node exactly at the middle has none, so it
        /// takes the fan's default direction.</summary>
        private static float Outward(float nodeX, float nodeY) =>
            nodeX == 0f && nodeY == 0f ? 0f : MathF.Atan2(nodeY, nodeX);

        private static float Offset(int index, int count, float arc) =>
            count <= 1 ? 0f : -arc * 0.5f + arc * index / (count - 1);

        private static float Radians(float degrees) => degrees * MathF.PI / 180f;
    }
}

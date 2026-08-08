namespace Core.PassiveTree.View
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// How big a node is on screen and which node a point on screen belongs to — one object, because
    /// the two answers are the same number read twice. A drawn radius with a floor under it and a
    /// picking radius without one is a node whose visible dot is larger than the area that answers a
    /// click, and nothing says so until somebody misses.
    /// <para>Everything is derived from <see cref="ScreenRadius"/>: the layer that draws the mass of
    /// nodes asks for it in document units, a node scene asks for the counter-scale that keeps it that
    /// size, a ring drawn around a node asks for a multiple of it, and the pick asks for it plus its
    /// slack. One table of authored radii, two floors, no second arithmetic anywhere.</para>
    /// <para>The look is not here. Shape, hue and texture differ between the game and the authoring
    /// tool; the geometry does not, so only the geometry moved.</para>
    /// </summary>
    public sealed class NodeGeometry
    {
        private readonly Dictionary<PassiveNodeKind, float> _radii = new();

        /// <summary>Reused across queries: picking runs on every mouse move, and a fresh list per move
        /// would make hovering the wheel an allocation loop.</summary>
        private readonly List<PassiveNode> _candidates = [];

        private readonly float _minScreenRadius;
        private readonly float _pickScreenSlack;

        /// <param name="authoredRadii">Radius per node class in document units, at zoom 1. Every class
        /// of the enum must have one: a missing entry would silently draw and pick at whatever the
        /// fallback happened to be.</param>
        /// <param name="minScreenRadius">How small a node may get on screen before it stops shrinking.
        /// Below it the whole tree on one screen turns into dust nobody can aim at.</param>
        /// <param name="pickScreenSlack">Pixels of reach added around a node when deciding what was
        /// clicked.</param>
        public NodeGeometry(IReadOnlyDictionary<PassiveNodeKind, float> authoredRadii, float minScreenRadius, float pickScreenSlack)
        {
            float largest = 0f;
            float smallest = float.MaxValue;

            foreach (PassiveNodeKind kind in Enum.GetValues<PassiveNodeKind>())
            {
                if (!authoredRadii.TryGetValue(kind, out float radius))
                    throw new InvalidOperationException($"NodeGeometry has no authored radius for {kind}");

                _radii[kind] = radius;
                if (radius > largest) largest = radius;
                if (radius < smallest) smallest = radius;
            }

            MaxAuthoredRadius = largest;
            MinAuthoredRadius = smallest;
            _minScreenRadius = minScreenRadius;
            _pickScreenSlack = pickScreenSlack;
        }

        /// <summary>The widest class on the board — the reach a pick query has to cover. Computed from
        /// the table so it can never become a second, stale constant.</summary>
        public float MaxAuthoredRadius { get; }

        /// <summary>The narrowest class — the one whose floor engages first as the view pulls out.</summary>
        public float MinAuthoredRadius { get; }

        public float AuthoredRadius(PassiveNodeKind kind) => _radii[kind];

        /// <summary>What the node measures on screen: its authored size at this zoom, never below the
        /// floor. Every other size in the wheel is this one divided by something.</summary>
        public float ScreenRadius(PassiveNodeKind kind, float zoom) =>
            MathF.Max(_radii[kind] * zoom, _minScreenRadius);

        /// <summary>The same size expressed in the units of the frame — what a layer drawing inside it
        /// has to hand the draw call, since the frame multiplies by its own scale again. The scale is
        /// asked for as a whole rather than as a zoom: the frame carries positions and therefore the
        /// spread as well, and a size divided by the zoom alone would be drawn a spread too large.</summary>
        public float DocumentRadius(PassiveNodeKind kind, ICanvasScale scale) =>
            scale.DocumentLength(ScreenRadius(kind, scale.Zoom));

        /// <summary>
        /// The radius of a ring drawn AROUND a node — the mark under the pointer, the one on what can be
        /// bought next — as a multiple of what the node actually measures on screen.
        /// <para>Never a fixed number of pixels outside it: <see cref="ScreenRadius"/> has a floor and a
        /// gap in pixels does not, so pulling the view out leaves the dot standing still at the floor
        /// while the ring keeps its distance, and the mark ends up several times the size of the thing
        /// it is about. A ring that big stops reading as that node's mark and starts reading as an
        /// object of its own.</para>
        /// </summary>
        public float ScreenRingRadius(PassiveNodeKind kind, float zoom, float ringScale) =>
            ScreenRadius(kind, zoom) * MathF.Max(ringScale, 1f);

        /// <summary>The same ring in the units of the frame, for a layer drawing inside it.</summary>
        public float DocumentRingRadius(PassiveNodeKind kind, ICanvasScale scale, float ringScale) =>
            scale.DocumentLength(ScreenRingRadius(kind, scale.Zoom, ringScale));

        /// <summary>The radius a click is measured against: what is drawn plus the slack that keeps a
        /// node aimable when the whole tree is on screen.</summary>
        public float PickRadius(PassiveNodeKind kind, float zoom) =>
            ScreenRadius(kind, zoom) + _pickScreenSlack;

        /// <summary>What a node scene must be scaled by inside the frame so it ends up
        /// <see cref="ScreenRadius"/> across on screen. A function of the class and the view alone —
        /// every node of a class is scaled by the same number, however many scenes the zoom step then
        /// walks through to write it. It falls below 1 wherever the frame is already blowing the layout
        /// up past the authored size, which is what a spread above 1 does.</summary>
        public float ViewScale(PassiveNodeKind kind, ICanvasScale scale)
        {
            float authored = _radii[kind];
            return authored <= 0f ? 1f : DocumentRadius(kind, scale) / authored;
        }

        /// <summary>
        /// The node under a point on screen, or null. Nearest centre wins among everything the point
        /// falls inside, so two overlapping shapes resolve by distance rather than by the order their
        /// author happened to type them into the file.
        /// </summary>
        public PassiveNode? At(PassiveTreeDocument document, CanvasTransform view, float screenX, float screenY)
        {
            float documentX = view.DocumentX(screenX);
            float documentY = view.DocumentY(screenY);

            // The grid is indexed in document coordinates, so the widest node on screen has to be asked
            // for in those — pixels of reach are worth less of the document the wider it is spread.
            float reach = view.DocumentLength(MaxAuthoredRadius * view.Zoom + _pickScreenSlack);

            _candidates.Clear();
            document.Index.Query(documentX - reach, documentY - reach, documentX + reach, documentY + reach, _candidates);

            PassiveNode? best = null;
            float bestDistance = float.MaxValue;

            foreach (PassiveNode node in _candidates)
            {
                float radius = PickRadius(node.Kind, view.Zoom);
                float deltaX = view.ScreenX(node.X) - screenX;
                float deltaY = view.ScreenY(node.Y) - screenY;
                float distance = deltaX * deltaX + deltaY * deltaY;

                if (distance > radius * radius || distance >= bestDistance) continue;

                best = node;
                bestDistance = distance;
            }

            return best;
        }
    }
}

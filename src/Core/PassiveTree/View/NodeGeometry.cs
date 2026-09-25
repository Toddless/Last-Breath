namespace Core.PassiveTree.View
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// A node's on-screen size and which node a screen point belongs to — one table, so drawing and
    /// picking never disagree (a floored draw radius and an unfloored pick radius would let the visible
    /// dot outgrow the clickable area). Everything else derives from <see cref="ScreenRadius"/> — node
    /// scale and ring radius as multiples of it, the pick radius as it plus its slack — so there is one
    /// table of authored radii, two floors and no second arithmetic anywhere. Geometry only: look
    /// (shape/hue/texture) lives elsewhere and differs between game and editor.
    /// </summary>
    public sealed class NodeGeometry
    {
        private readonly Dictionary<PassiveNodeKind, float> _radii = new();

        /// <summary>Reused across queries: picking runs on every mouse move, so a fresh list per move
        /// would allocate on every hover.</summary>
        private readonly List<PassiveNode> _candidates = [];

        private readonly float _minScreenRadius;
        private readonly float _pickScreenSlack;

        /// <param name="authoredRadii">Radius per node class in document units at zoom 1. Every enum
        /// member must have an entry, or it would silently draw/pick at a fallback size.</param>
        /// <param name="minScreenRadius">Floor under a node's on-screen size, so a zoomed-out tree does
        /// not turn to unaimable dust.</param>
        /// <param name="pickScreenSlack">Pixels of reach added around a node for click detection.</param>
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

        /// <summary>Widest class on the board — the reach a pick query must cover.</summary>
        public float MaxAuthoredRadius { get; }

        /// <summary>Narrowest class — whose floor engages first as the view zooms out.</summary>
        public float MinAuthoredRadius { get; }

        public float AuthoredRadius(PassiveNodeKind kind) => _radii[kind];

        /// <summary>The node's on-screen size: authored size at this zoom, never below the floor.</summary>
        public float ScreenRadius(PassiveNodeKind kind, float zoom) =>
            MathF.Max(_radii[kind] * zoom, _minScreenRadius);

        /// <summary>Same size in frame units. Divides by the full scale (zoom*spread), not zoom alone —
        /// the frame carries positions, so a size divided by zoom alone would draw a spread too large.</summary>
        public float DocumentRadius(PassiveNodeKind kind, ICanvasScale scale) =>
            scale.DocumentLength(ScreenRadius(kind, scale.Zoom));

        /// <summary>
        /// Radius of a ring drawn around a node (hover mark, next-purchase mark), as a multiple of what
        /// the node actually measures on screen — never a fixed pixel gap. <see cref="ScreenRadius"/> has
        /// a floor and a pixel gap does not, so at low zoom the dot sits at the floor while a fixed gap
        /// keeps its distance, blowing the ring out of proportion to the node it marks.
        /// </summary>
        public float ScreenRingRadius(PassiveNodeKind kind, float zoom, float ringScale) =>
            ScreenRadius(kind, zoom) * MathF.Max(ringScale, 1f);

        /// <summary>Same ring in frame units.</summary>
        public float DocumentRingRadius(PassiveNodeKind kind, ICanvasScale scale, float ringScale) =>
            scale.DocumentLength(ScreenRingRadius(kind, scale.Zoom, ringScale));

        /// <summary>What a click is measured against: drawn radius plus slack.</summary>
        public float PickRadius(PassiveNodeKind kind, float zoom) =>
            ScreenRadius(kind, zoom) + _pickScreenSlack;

        /// <summary>Scale factor for a node scene so it renders at <see cref="ScreenRadius"/>. Falls
        /// below 1 wherever spread above 1 is already enlarging the layout past the authored size.</summary>
        public float ViewScale(PassiveNodeKind kind, ICanvasScale scale)
        {
            float authored = _radii[kind];
            return authored <= 0f ? 1f : DocumentRadius(kind, scale) / authored;
        }

        /// <summary>The node under a screen point, or null. Nearest centre wins among everything the
        /// point falls inside, so overlapping shapes resolve by distance, not authoring order.</summary>
        public PassiveNode? At(PassiveTreeDocument document, CanvasTransform view, float screenX, float screenY)
        {
            float documentX = view.DocumentX(screenX);
            float documentY = view.DocumentY(screenY);

            // Grid is indexed in document coordinates, so pixel reach is converted before querying —
            // spread changes how much document a pixel covers.
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

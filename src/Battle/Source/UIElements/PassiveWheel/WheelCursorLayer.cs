namespace Battle.Source.UIElements.PassiveWheel
{
    using System;
    using System.Collections.Generic;
    using Core.PassiveTree;
    using Core.PassiveTree.View;
    using Godot;

    /// <summary>
    /// Everything that answers the cursor: the ring around what can be bought next, the ring under the
    /// pointer and the dashed route to it. The most frequently invalidated drawing in the window is
    /// isolated in the layer with the fewest primitives, and it serves nodes with and without a scene
    /// alike — the hover rule is written once here instead of once per node.
    /// <para>A ring here is measured off the node it marks (<see cref="NodeGeometry.DocumentRingRadius"/>)
    /// and not off a gap in pixels. The node's size has a floor under it: a constant gap outlives that
    /// floor and the mark ends up several times the dot it is about, which reads as an object of its
    /// own rather than as "this one". The ring WIDTHS go the other way and are held in screen pixels —
    /// a mark on the thing under the pointer is an affordance and not part of the map, so it keeps its
    /// weight however deep the wheel is zoomed. The dashed route is part of the map and scales with
    /// it.</para>
    /// </summary>
    [GlobalClass]
    public partial class WheelCursorLayer : Node2D
    {
        /// <summary>How much bigger than the node itself each ring is drawn. The pointer's sits outside
        /// the frontier's so that the two never coincide on the node that wears both.</summary>
        private const float FrontierRingScale = 1.3f;

        private const float HoverRingScale = 1.55f;

        /// <summary>Ring weights in screen pixels. The pointer's is the heavier of the two: there is one
        /// of it and it answers a question the player is asking right now, while the frontier is a
        /// standing mark on everything within reach.</summary>
        private const float FrontierRingWidth = 1.5f;

        private const float HoverRingWidth = 2.5f;

        /// <summary>Straight pieces per screen pixel of radius, and the ends of the range. A ring drawn
        /// in document units is blown up by the frame, so a count fixed in the source is a circle at one
        /// zoom and a visible polygon at another.</summary>
        private const float SegmentsPerPixel = 1.2f;

        private const int FewestSegments = 10;
        private const int MostSegments = 48;
        private const float DashLength = 7f;

        [Export] private PassiveWheelStyle? _style;

        private readonly HashSet<string> _frontier = new(StringComparer.Ordinal);
        private readonly HashSet<string> _taken = new(StringComparer.Ordinal);
        private readonly List<string> _path = [];

        private PassiveTreeDocument? _document;
        private NodeGeometry? _geometry;
        private string? _hovered;
        private float _zoom = 1f;

        public void SetDocument(PassiveTreeDocument? document)
        {
            _document = document;
            QueueRedraw();
        }

        public void SetGeometry(NodeGeometry? geometry)
        {
            _geometry = geometry;
            QueueRedraw();
        }

        /// <summary>What the allocation says can be bought right now. Asked of the same check a click
        /// goes through, so the ring can never disagree with what taking the node does.</summary>
        public void SetFrontier(IReadOnlyCollection<string> frontier)
        {
            _frontier.Clear();
            foreach (string id in frontier) _frontier.Add(id);
            QueueRedraw();
        }

        /// <summary>What the character already holds — the layer's own copy, and only so that the
        /// dashed route knows which end it hangs off.</summary>
        public void SetTaken(IReadOnlyCollection<string> taken)
        {
            _taken.Clear();
            foreach (string id in taken) _taken.Add(id);
            QueueRedraw();
        }

        public void SetHovered(PassiveNode? node)
        {
            _hovered = node?.Id;
            QueueRedraw();
        }

        /// <summary>The cheapest route to the hovered node, the service's own answer. The layer never
        /// walks the graph itself.</summary>
        public void SetPath(IReadOnlyList<string> route)
        {
            _path.Clear();
            _path.AddRange(route);
            QueueRedraw();
        }

        public void SetZoom(float zoom)
        {
            _zoom = zoom;
            QueueRedraw();
        }

        public override void _Draw()
        {
            if (_document == null || _style == null || _geometry == null) return;

            DrawPath();
            DrawFrontier();
            DrawHover();
        }

        private void DrawFrontier()
        {
            if (_document == null || _style == null || _geometry == null) return;

            foreach (string id in _frontier)
            {
                // The node under the pointer wears the pointer's ring instead: on a small node the two
                // are a pixel apart and read as one thick smudge rather than as two answers.
                if (string.Equals(id, _hovered, StringComparison.Ordinal)) continue;

                PassiveNode? node = _document.Find(id);
                if (node == null) continue;

                Ring(node, FrontierRingScale, _style.Frontier, FrontierRingWidth);
            }
        }

        private void DrawHover()
        {
            if (_hovered == null || _document == null || _style == null || _geometry == null) return;

            PassiveNode? node = _document.Find(_hovered);
            if (node == null) return;

            Ring(node, HoverRingScale, _style.Hover, HoverRingWidth);
        }

        /// <summary>The one place a ring around a node is drawn, so the pointer's mark and the frontier's
        /// cannot come to be measured two different ways.</summary>
        private void Ring(PassiveNode node, float scale, Color color, float screenWidth)
        {
            if (_geometry == null) return;

            DrawArc(new Vector2(node.X, node.Y), _geometry.DocumentRingRadius(node.Kind, _zoom, scale),
                0f, Mathf.Tau, Segments(_geometry.ScreenRingRadius(node.Kind, _zoom, scale)),
                color, FromScreen(screenWidth), true);
        }

        /// <summary>The route as a dashed line through the nodes still to be bought, starting from
        /// whatever taken node it hangs off.</summary>
        private void DrawPath()
        {
            if (_document == null || _style == null || _path.Count == 0) return;

            float width = _style.DocumentEdgeWidth(_style.TakenEdgeWidth, _zoom);
            float dash = FromScreen(DashLength);

            for (int step = 0; step < _path.Count; step++)
            {
                PassiveNode? node = _document.Find(_path[step]);
                if (node == null) continue;

                PassiveNode? previous = step == 0 ? AnchorOf(node) : _document.Find(_path[step - 1]);
                if (previous == null) continue;

                DrawDashedLine(new Vector2(previous.X, previous.Y), new Vector2(node.X, node.Y),
                    _style.EdgePath, width, dash);
            }
        }

        /// <summary>Where the route leaves the allocation: the first step of a path always touches
        /// something already taken, and that neighbour is the end the dashes start from.</summary>
        private PassiveNode? AnchorOf(PassiveNode first)
        {
            if (_document == null) return null;

            foreach (string neighbourId in _document.Neighbours(first.Id))
                if (_taken.Contains(neighbourId))
                    return _document.Find(neighbourId);

            return null;
        }

        /// <summary>Enough straight pieces for the ring to read as one at the size it is being seen at:
        /// a dozen is a circle at three pixels across and a visible polygon at sixty.</summary>
        private static int Segments(float screenRadius) =>
            Math.Clamp((int)MathF.Ceiling(screenRadius * SegmentsPerPixel), FewestSegments, MostSegments);

        /// <summary>A length authored in screen pixels, in the document units the frame will scale.</summary>
        private float FromScreen(float pixels) => _zoom <= 0f ? pixels : pixels / _zoom;
    }
}

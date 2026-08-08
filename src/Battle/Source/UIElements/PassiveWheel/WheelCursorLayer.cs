namespace Battle.Source.UIElements.PassiveWheel
{
    using System;
    using System.Collections.Generic;
    using Core.PassiveTree;
    using Core.PassiveTree.View;
    using Godot;

    /// <summary>Which half of the cursor's answer a layer draws. Two instances of one class rather than
    /// two classes, the way <see cref="WheelEdgeLayer"/> is already split: the arithmetic is the same and
    /// only the drawing ORDER differs.</summary>
    public enum CursorLayerRole
    {
        /// <summary>The dashed route to the node under the pointer, the ring on what can be bought next
        /// and the outline of the plan. Drawn UNDER the node scenes — a route is part of the map and must
        /// not be laid over the things it connects.</summary>
        Route,

        /// <summary>The gold rim of the node under the pointer, and nothing else. Drawn OVER the node
        /// scenes: it follows the edge of the node's own body, and under the scene it would be hidden by
        /// the very sprite it outlines.</summary>
        Rim
    }

    /// <summary>
    /// Everything that answers the cursor: the ring around what can be bought next, the gold rim under
    /// the pointer, the dashed route to it, and the plan — its marks and the outline over its links. The
    /// most frequently invalidated drawing in the window is isolated in the layer with the fewest
    /// primitives, and it serves nodes with and without a scene alike — the hover rule and the mark of a
    /// planned node are written once here instead of once per node and once per layer.
    /// <para>A ring here is measured off the node it marks (<see cref="NodeGeometry.DocumentRingRadius"/>)
    /// and not off a gap in pixels. The node's size has a floor under it: a constant gap outlives that
    /// floor and the mark ends up several times the dot it is about, which reads as an object of its
    /// own rather than as "this one". The ring WIDTHS go the other way and are held in screen pixels —
    /// a mark on the thing under the pointer is an affordance and not part of the map, so it keeps its
    /// weight however deep the wheel is zoomed. The dashed route is part of the map and scales with
    /// it.</para>
    /// <para>The pointer's mark sits at the node's OWN radius: it is the edge of the thing the player
    /// aimed at, so it lands exactly where <see cref="NodeGeometry.At"/> measured the click and exactly
    /// where the body is drawn. That is also why the frontier ring, which sits outside the node, is no
    /// longer skipped on the hovered node — the two are on different radii and on opposite sides of the
    /// body's edge, and the player has to see both answers at once.</para>
    /// </summary>
    [GlobalClass]
    public partial class WheelCursorLayer : Node2D
    {
        /// <summary>How much bigger than the node itself each mark is drawn. The frontier's sits outside
        /// the body; the pointer's is the body's own edge, which
        /// <see cref="NodeGeometry.ScreenRingRadius"/> clamps to for free.</summary>
        private const float FrontierRingScale = 1.3f;

        private const float HoverRimScale = 1f;

        /// <summary>Ring weights in screen pixels. The pointer's is the heavier of the two: there is one
        /// of it and it answers a question the player is asking right now, while the frontier is a
        /// standing mark on everything within reach.</summary>
        private const float FrontierRingWidth = 1.5f;

        private const float HoverRimWidth = 2.5f;

        /// <summary>Straight pieces per screen pixel of radius, and the ends of the range. A ring drawn
        /// in document units is blown up by the frame, so a count fixed in the source is a circle at one
        /// zoom and a visible polygon at another.</summary>
        private const float SegmentsPerPixel = 1.2f;

        private const int FewestSegments = 10;
        private const int MostSegments = 48;
        private const float DashLength = 7f;

        [Export] private CursorLayerRole _role;
        [Export] private PassiveWheelStyle? _style;

        private readonly HashSet<string> _frontier = new(StringComparer.Ordinal);
        private readonly HashSet<string> _taken = new(StringComparer.Ordinal);
        private readonly HashSet<string> _plan = new(StringComparer.Ordinal);
        private readonly List<string> _path = [];

        private PassiveTreeDocument? _document;
        private NodeGeometry? _geometry;
        private ICanvasScale? _scale;
        private string? _hovered;
        private bool _planGivesBack;

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
            if (_role != CursorLayerRole.Route) return;

            _frontier.Clear();
            foreach (string id in frontier) _frontier.Add(id);
            QueueRedraw();
        }

        /// <summary>What the character already holds — the layer's own copy, and only so that the
        /// dashed route knows which end it hangs off.</summary>
        public void SetTaken(IReadOnlyCollection<string> taken)
        {
            if (_role != CursorLayerRole.Route) return;

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
            if (_role != CursorLayerRole.Route) return;

            _path.Clear();
            _path.AddRange(route);
            QueueRedraw();
        }

        /// <summary>The nodes the plan touches, and which way it points. Both halves arrive as one call:
        /// a plan is one set with one direction, and a layer holding the two separately could draw a
        /// purchase in the colour of a return for one frame.</summary>
        public void SetPlan(IReadOnlyCollection<string> planned, bool givesBack)
        {
            if (_role != CursorLayerRole.Route) return;

            _plan.Clear();
            foreach (string id in planned) _plan.Add(id);
            _planGivesBack = givesBack;
            QueueRedraw();
        }

        public void SetScale(ICanvasScale scale)
        {
            _scale = scale;
            QueueRedraw();
        }

        public override void _Draw()
        {
            if (_document == null || _style == null || _geometry == null || _scale == null) return;

            if (_role == CursorLayerRole.Rim)
            {
                DrawHoverRim();
                return;
            }

            DrawPlan();
            DrawPath();
            DrawFrontier();
        }

        private void DrawFrontier()
        {
            if (_document == null || _style == null || _geometry == null) return;

            foreach (string id in _frontier)
            {
                PassiveNode? node = _document.Find(id);
                if (node == null) continue;

                Ring(node, FrontierRingScale, _style.Frontier, FrontierRingWidth);
            }
        }

        private void DrawHoverRim()
        {
            if (_hovered == null || _document == null || _style == null || _geometry == null) return;

            PassiveNode? node = _document.Find(_hovered);
            if (node == null) return;

            Ring(node, HoverRimScale, _style.Hover, HoverRimWidth);
        }

        /// <summary>The one place a ring around a node is drawn, so the pointer's mark, the frontier's and
        /// the plan's cannot come to be measured three different ways.</summary>
        private void Ring(PassiveNode node, float ringScale, Color color, float screenWidth)
        {
            if (_geometry == null || _scale == null) return;

            DrawArc(new Vector2(node.X, node.Y), _geometry.DocumentRingRadius(node.Kind, _scale, ringScale),
                0f, Mathf.Tau, Segments(_geometry.ScreenRingRadius(node.Kind, _scale.Zoom, ringScale)),
                color, FromScreen(screenWidth), true);
        }

        /// <summary>The route as a dashed line through the nodes still to be bought, starting from
        /// whatever taken node it hangs off.</summary>
        private void DrawPath()
        {
            if (_document == null || _style == null || _scale == null || _path.Count == 0) return;

            float width = _style.DocumentEdgeWidth(_style.TakenEdgeWidth, _scale);
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

        /// <summary>
        /// The plan: a mark on every node it touches, and a dashed outline over the links inside it, in
        /// the colour of what the button is about to do. Each link is drawn from one of its two ends
        /// only, the way the taken edges are, so a line is never laid over itself.
        /// <para>The MARK is the whole of what "this node is in the plan" looks like, and this is the one
        /// place it is drawn. A single mark has no link to hang a dashed line off, and most of the road
        /// is made of the small class that is drawn as part of the mass — so a plan drawn only as edges,
        /// or only inside the node scenes, is a click that leaves the screen unchanged.</para>
        /// </summary>
        private void DrawPlan()
        {
            if (_document == null || _style == null || _scale == null || _plan.Count == 0) return;

            float width = _style.DocumentEdgeWidth(_style.TakenEdgeWidth, _scale);
            float dash = FromScreen(DashLength);
            PassiveNodeVisualState state = PassiveNodeStates.OfPlanned(_planGivesBack);
            PassiveNodeMark mark = PassiveNodeStates.MarkOf(state);
            Color color = state == PassiveNodeVisualState.PendingRefund ? _style.PlanRefund : _style.PlanTake;

            foreach (string id in _plan)
            {
                PassiveNode? node = _document.Find(id);
                if (node == null) continue;

                if (mark.IsDrawn) Ring(node, mark.RingScale, color, mark.ScreenWidth);

                foreach (string neighbourId in _document.Neighbours(id))
                {
                    if (!_plan.Contains(neighbourId) || string.CompareOrdinal(id, neighbourId) > 0) continue;

                    PassiveNode? neighbour = _document.Find(neighbourId);
                    if (neighbour == null) continue;

                    DrawDashedLine(new Vector2(node.X, node.Y), new Vector2(neighbour.X, neighbour.Y),
                        color, width, dash);
                }
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

        /// <summary>A length authored in screen pixels, in the units the frame will scale back up.</summary>
        private float FromScreen(float pixels) => _scale?.DocumentLength(pixels) ?? pixels;
    }
}

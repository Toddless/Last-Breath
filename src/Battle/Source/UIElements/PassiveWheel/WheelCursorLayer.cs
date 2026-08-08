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
    /// </summary>
    [GlobalClass]
    public partial class WheelCursorLayer : Node2D
    {
        private const int RingSegments = 24;
        private const float FrontierGap = 3f;
        private const float HoverGap = 5f;
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

            float width = _style.DocumentEdgeWidth(_style.IdleEdgeWidth, _zoom);

            foreach (string id in _frontier)
            {
                PassiveNode? node = _document.Find(id);
                if (node == null) continue;

                DrawArc(new Vector2(node.X, node.Y), _geometry.DocumentRadius(node.Kind, _zoom) + Gap(FrontierGap),
                    0f, Mathf.Tau, RingSegments, _style.Frontier, width, true);
            }
        }

        private void DrawHover()
        {
            if (_hovered == null || _document == null || _style == null || _geometry == null) return;

            PassiveNode? node = _document.Find(_hovered);
            if (node == null) return;

            DrawArc(new Vector2(node.X, node.Y), _geometry.DocumentRadius(node.Kind, _zoom) + Gap(HoverGap),
                0f, Mathf.Tau, RingSegments, _style.Hover,
                _style.DocumentEdgeWidth(_style.TakenEdgeWidth, _zoom), true);
        }

        /// <summary>The route as a dashed line through the nodes still to be bought, starting from
        /// whatever taken node it hangs off.</summary>
        private void DrawPath()
        {
            if (_document == null || _style == null || _path.Count == 0) return;

            float width = _style.DocumentEdgeWidth(_style.TakenEdgeWidth, _zoom);
            float dash = Gap(DashLength);

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

        /// <summary>A gap authored in screen pixels, in the document units the frame will scale.</summary>
        private float Gap(float screenGap) => _zoom <= 0f ? screenGap : screenGap / _zoom;
    }
}

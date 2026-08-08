namespace Battle.Source.UIElements.PassiveWheel
{
    using System;
    using System.Collections.Generic;
    using Core.PassiveTree;
    using Godot;

    /// <summary>Which half of the graph a layer draws.</summary>
    public enum EdgeLayerRole
    {
        /// <summary>Every link of the document, dim. Changes with the document and with nothing else —
        /// buying a node does not move a single line of it.</summary>
        All,

        /// <summary>The links whose both ends are taken, drawn opaque over the dim ones. The only edge
        /// drawing an allocation change touches.</summary>
        Taken
    }

    /// <summary>
    /// Edges in document units inside the frame. Split in two by how often they change rather than by
    /// how they look: the dim mesh of the whole tree is drawn once and then only moved by the frame,
    /// while the allocation redraws a set the size of the point budget.
    /// </summary>
    [GlobalClass]
    public partial class WheelEdgeLayer : Node2D
    {
        [Export] private EdgeLayerRole _role;
        [Export] private PassiveWheelStyle? _style;

        private readonly HashSet<string> _taken = new(StringComparer.Ordinal);

        private PassiveTreeDocument? _document;
        private float _zoom = 1f;

        public void SetDocument(PassiveTreeDocument? document)
        {
            _document = document;
            QueueRedraw();
        }

        /// <summary>Read by the taken role alone. Kept as the layer's own copy: the set it is handed is
        /// the service's live collection, and a redraw happening a frame later would walk it while
        /// somebody else is changing it.</summary>
        public void SetTaken(IReadOnlyCollection<string> taken)
        {
            if (_role != EdgeLayerRole.Taken) return;

            _taken.Clear();
            foreach (string id in taken) _taken.Add(id);
            QueueRedraw();
        }

        public void SetZoom(float zoom)
        {
            _zoom = zoom;
            QueueRedraw();
        }

        public override void _Draw()
        {
            if (_document == null || _style == null) return;

            if (_role == EdgeLayerRole.Taken) DrawTaken();
            else DrawAll();
        }

        private void DrawAll()
        {
            if (_document == null || _style == null) return;

            float width = _style.DocumentEdgeWidth(_style.IdleEdgeWidth, _zoom);

            foreach (NodeLink link in _document.Links)
            {
                PassiveNode? first = _document.Find(link.A);
                PassiveNode? second = _document.Find(link.B);
                if (first == null || second == null) continue;

                DrawLine(At(first), At(second), _style.EdgeIdle, width);
            }
        }

        /// <summary>Walks the taken nodes and their neighbours rather than every link in the document:
        /// there are never more taken nodes than the budget allows, however big the tree grows.</summary>
        private void DrawTaken()
        {
            if (_document == null || _style == null) return;

            float width = _style.DocumentEdgeWidth(_style.TakenEdgeWidth, _zoom);

            foreach (string id in _taken)
            {
                PassiveNode? node = _document.Find(id);
                if (node == null) continue;

                foreach (string neighbourId in _document.Neighbours(id))
                {
                    // Each edge belongs to two taken nodes; the ordinal comparison draws it from one of
                    // them only, so a line is never laid over itself.
                    if (!_taken.Contains(neighbourId) || string.CompareOrdinal(id, neighbourId) > 0) continue;

                    PassiveNode? neighbour = _document.Find(neighbourId);
                    if (neighbour == null) continue;

                    DrawLine(At(node), At(neighbour), _style.EdgeTaken, width);
                }
            }
        }

        private static Vector2 At(PassiveNode node) => new(node.X, node.Y);
    }
}

namespace Battle.Source.UIElements.PassiveWheel
{
    using System;
    using System.Collections.Generic;
    using Core.PassiveTree;
    using Core.PassiveTree.View;
    using Godot;

    /// <summary>
    /// The mass of the wheel: every node that does NOT carry a scene of its own. It draws exactly the
    /// complement of the carrier set it is handed, so a node is drawn here or as a scene and never
    /// both — the two are the same one pass over the allocation, not two answers to the same question.
    /// <para>Nothing in here is ever taken: taking a node makes it a carrier by definition, which is
    /// why the mass never has to pulse or animate.</para>
    /// <para>Nor does it know anything about the PLAN. What the plan touches is marked over the whole
    /// field by <see cref="WheelCursorLayer"/>, so a node wears the same mark whether it is drawn here or
    /// as a scene, and marking one costs this layer nothing beyond the redraw the carrier set already
    /// asks for.</para>
    /// </summary>
    [GlobalClass]
    public partial class WheelFieldLayer : Node2D
    {
        private const int CircleSegments = 12;

        [Export] private PassiveWheelStyle? _style;

        private readonly HashSet<string> _carriers = new(StringComparer.Ordinal);

        private PassiveTreeDocument? _document;
        private NodeGeometry? _geometry;
        private ICanvasScale? _scale;
        private PassiveNodeVisualIndex<PassiveNodeVisualConfig>? _visuals;

        public void SetDocument(PassiveTreeDocument? document)
        {
            _document = document;
            QueueRedraw();
        }

        /// <summary>The authored looks, or null when the wheel carries no library. Handed down by the
        /// canvas rather than exported here, so one assignment in the editor serves every layer and the
        /// mass cannot end up reading a different resource from the scenes above it.</summary>
        public void SetVisuals(PassiveNodeVisualIndex<PassiveNodeVisualConfig>? visuals)
        {
            _visuals = visuals;
            QueueRedraw();
        }

        public void SetGeometry(NodeGeometry? geometry)
        {
            _geometry = geometry;
            QueueRedraw();
        }

        /// <summary>The ids that got a scene. Everything else in the document is drawn here.</summary>
        public void SetCarriers(IReadOnlyCollection<string> carriers)
        {
            _carriers.Clear();
            foreach (string id in carriers) _carriers.Add(id);
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

            float outline = _style.DocumentEdgeWidth(_style.IdleEdgeWidth, _scale);

            foreach (PassiveNode node in _document.Nodes)
            {
                if (_carriers.Contains(node.Id)) continue;

                var centre = new Vector2(node.X, node.Y);
                float radius = _geometry.DocumentRadius(node.Kind, _scale);
                PassiveNodeVisualConfig? art = _visuals?.For(node);
                Color ray = _style.ColorOf(node);

                DrawFace(centre, radius, art, ray);

                // The outline is the CLASS drawing, so it wears the ray whatever the row put inside it —
                // one reading with the scenes above, through PassiveNodeVisualConfig.ModulateOf.
                DrawArc(centre, radius, 0f, Mathf.Tau, CircleSegments,
                    new Color(PassiveNodeVisualConfig.ModulateOf(art, authored: false, ray), _style.NodeIdleOutline.A),
                    outline, true);
            }
        }

        /// <summary>What fills the node: the authored face when a row carries one, otherwise the flat
        /// disc the mass has always been. What colours it is not decided here — see
        /// <see cref="PassiveNodeVisualConfig.ModulateOf"/>, which the node scenes read too, so one PNG
        /// does not come out one colour on a class drawn as a scene and another on a class drawn here.
        /// <para>The OUTLINE is drawn either way, so a node in the mass never stops saying which ray it
        /// belongs to.</para></summary>
        private void DrawFace(Vector2 centre, float radius, PassiveNodeVisualConfig? art, Color ray)
        {
            if (art?.Body is not { } face)
            {
                DrawCircle(centre, radius, _style!.NodeIdleFill);
                return;
            }

            DrawTextureRect(face, new Rect2(centre - new Vector2(radius, radius), new Vector2(radius * 2f, radius * 2f)),
                false, PassiveNodeVisualConfig.ModulateOf(art, authored: true, ray));
        }
    }
}

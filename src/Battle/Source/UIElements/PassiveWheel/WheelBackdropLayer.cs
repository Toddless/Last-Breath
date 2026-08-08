namespace Battle.Source.UIElements.PassiveWheel
{
    using System;
    using Core.Localization;
    using Godot;

    /// <summary>
    /// The wheel behind the tree: six wedges, the ring radii and the glow at the core. Drawn in
    /// document units inside the frame, so panning and zooming cost it nothing — a redraw happens only
    /// when the zoom crosses a screen floor and the ring line would otherwise vanish.
    /// </summary>
    [GlobalClass]
    public partial class WheelBackdropLayer : Node2D
    {
        /// <summary>Godot draws solid arcs, so a dashed ring is every other segment of a coarse circle
        /// — cheap and visually identical at these radii.</summary>
        private const int RingSegments = 144;

        private const int WedgeSegments = 14;
        private const int GlowLayers = 7;
        private const float WedgeAlpha = 0.07f;
        private const float LabelOffset = 28f;
        private const float LabelWidth = 240f;

        [Export] private PassiveWheelStyle? _style;

        private float _zoom = 1f;
        private bool _labels;

        /// <summary>The only thing a layer is ever told. It is never given the pan or the viewport
        /// size, so it cannot start culling — and it has no reason to: the frame moves, not the
        /// drawing.</summary>
        public void SetZoom(float zoom, bool labels)
        {
            _zoom = zoom;
            _labels = labels;
            QueueRedraw();
        }

        public override void _Draw()
        {
            if (_style == null) return;

            DrawWedges();
            DrawRings();
            DrawCoreGlow();
            if (_labels) DrawWedgeLabels();
        }

        private void DrawWedges()
        {
            if (_style == null) return;

            float radius = _style.SectorRadius;
            float half = _style.SectorHalfAngleDegrees;

            foreach (WheelSector sector in _style.Sectors)
            {
                var points = new Vector2[WedgeSegments + 2];
                points[0] = Vector2.Zero;

                for (int step = 0; step <= WedgeSegments; step++)
                {
                    float degrees = sector.AngleDegrees - half + 2f * half * step / WedgeSegments;
                    float radians = Mathf.DegToRad(degrees);
                    points[step + 1] = new Vector2(MathF.Cos(radians), MathF.Sin(radians)) * radius;
                }

                DrawColoredPolygon(points, new Color(sector.Tint, WedgeAlpha));
            }
        }

        private void DrawRings()
        {
            if (_style == null) return;

            float width = _style.DocumentEdgeWidth(_style.RingWidth, _zoom);

            foreach (float radius in _style.RingRadii)
            {
                if (radius <= 0f) continue;

                for (int step = 0; step < RingSegments; step += 2)
                {
                    float from = Mathf.Tau * step / RingSegments;
                    float to = Mathf.Tau * (step + 1) / RingSegments;
                    DrawArc(Vector2.Zero, radius, from, to, 2, _style.RingLine, width, true);
                }
            }
        }

        /// <summary>Stand-in for a radial gradient: a few nested discs of falling alpha.</summary>
        private void DrawCoreGlow()
        {
            if (_style == null || _style.CoreGlowRadius <= 0f) return;

            for (int layer = GlowLayers; layer > 0; layer--)
            {
                float factor = (float)layer / GlowLayers;
                DrawCircle(Vector2.Zero, _style.CoreGlowRadius * factor,
                    new Color(_style.CoreGlow, 0.10f * (1f - factor) + 0.04f));
            }
        }

        private void DrawWedgeLabels()
        {
            if (_style == null) return;

            Font? font = ThemeDB.Singleton.FallbackFont;
            if (font == null) return;

            // Drawn inside the frame, so a point size has to be divided by the zoom the frame will
            // multiply it back by — otherwise the wedge names grow and shrink with the wheel.
            float scale = _zoom <= 0f ? 1f : 1f / _zoom;
            int fontSize = Math.Max(1, (int)(_style.LabelFontSize * scale));
            float width = LabelWidth * scale;
            float radius = _style.SectorRadius + LabelOffset;

            foreach (WheelSector sector in _style.Sectors)
            {
                if (sector.LabelKey.Length == 0) continue;

                float radians = Mathf.DegToRad(sector.AngleDegrees);
                Vector2 at = new Vector2(MathF.Cos(radians), MathF.Sin(radians)) * radius;
                DrawString(font, new Vector2(at.X - width * 0.5f, at.Y), Localization.Localize(sector.LabelKey),
                    HorizontalAlignment.Center, width, fontSize, sector.Tint);
            }
        }
    }
}

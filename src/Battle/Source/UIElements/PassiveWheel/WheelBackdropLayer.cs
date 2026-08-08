namespace Battle.Source.UIElements.PassiveWheel
{
    using System;
    using Core.Localization;
    using Core.PassiveTree.View;
    using Godot;

    /// <summary>
    /// The wheel behind the tree: six wedges, the ring radii and the glow at the core. Drawn in
    /// document units inside the frame, so panning costs it nothing; a zoom costs it one redraw,
    /// because the ring line is floored in screen pixels and the wedge names are glyphs, and neither
    /// survives being scaled instead of drawn. An allocation pass costs it the same redraw — the canvas
    /// hands the zoom down at the end of one — over a picture nothing about it can have changed.
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

        private ICanvasScale? _scale;
        private bool _labels;

        /// <summary>The only thing a layer is ever told, and it is the two readings of scale rather than
        /// the transform itself: no layer is given the pan or the viewport size, so none of them can
        /// start culling — and none has a reason to, because the frame moves and the drawing does
        /// not.</summary>
        public void SetScale(ICanvasScale scale, bool labels)
        {
            _scale = scale;
            _labels = labels;
            QueueRedraw();
        }

        public override void _Draw()
        {
            if (_style == null || _scale == null) return;

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
            if (_style == null || _scale == null) return;

            float width = _style.DocumentEdgeWidth(_style.RingWidth, _scale);

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

        /// <summary>
        /// The wedge names, at the point size they were authored at whatever the zoom.
        /// <para>A glyph is rasterised at the size it is asked for and the frame then scales the result,
        /// so asking for a smaller size at a deeper zoom only makes a smaller picture to blow up — which
        /// is what turned the names into blurred giants. The size asked for is the authored one and the
        /// DRAWING is counter-scaled instead: inside the counter-scale one unit is one screen pixel, the
        /// frame multiplies it back to exactly one, and the glyph lands on screen at the size it was
        /// rasterised at.</para>
        /// </summary>
        private void DrawWedgeLabels()
        {
            if (_style == null || _scale == null) return;

            Font? font = ThemeDB.Singleton.FallbackFont;
            if (font == null) return;

            // One unit inside the counter-scale is one screen pixel, so it is the FRAME's scale that is
            // undone here and not the zoom: at a spread the frame carries, undoing only the zoom would
            // leave every caption a spread too large. The offset it is placed at, by contrast, is a
            // distance in the layout and stays in the layout's own units.
            float counter = _scale.DocumentLength(1f);
            float radius = _style.SectorRadius + LabelOffset;

            foreach (WheelSector sector in _style.Sectors)
            {
                if (sector.LabelKey.Length == 0) continue;

                float radians = Mathf.DegToRad(sector.AngleDegrees);
                Vector2 at = new Vector2(MathF.Cos(radians), MathF.Sin(radians)) * radius;

                DrawSetTransform(at, 0f, new Vector2(counter, counter));
                DrawString(font, new Vector2(-LabelWidth * 0.5f, 0f), Localization.Localize(sector.LabelKey),
                    HorizontalAlignment.Center, LabelWidth, _style.LabelFontSize, sector.Tint);
            }

            // The transform outlives the call that set it, so the next thing drawn on this layer would
            // inherit a counter-scale that has nothing to do with it.
            DrawSetTransform(Vector2.Zero, 0f, Vector2.One);
        }
    }
}

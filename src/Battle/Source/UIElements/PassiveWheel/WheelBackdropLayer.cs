namespace Battle.Source.UIElements.PassiveWheel
{
    using System;
    using Core.PassiveTree.View;
    using Godot;

    /// <summary>
    /// The wheel behind the tree: six wedges, the ring radii and the glow at the core. Drawn in
    /// document units inside the frame, so panning costs it nothing; a zoom costs it one redraw,
    /// because the ring line is floored in screen pixels and a floor only holds at the zoom it was
    /// measured for. An allocation pass costs it the same redraw — the canvas hands the zoom down at the
    /// end of one — over a picture nothing about it can have changed.
    /// <para>The wedges carry no names. A ray is read by where it points and what colour it is; six
    /// words floating over the tree were a second map laid on top of the first.</para>
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

        [Export] private PassiveWheelStyle? _style;

        private ICanvasScale? _scale;

        /// <summary>The only thing a layer is ever told, and it is the two readings of scale rather than
        /// the transform itself: no layer is given the pan or the viewport size, so none of them can
        /// start culling — and none has a reason to, because the frame moves and the drawing does
        /// not.</summary>
        public void SetScale(ICanvasScale scale)
        {
            _scale = scale;
            QueueRedraw();
        }

        public override void _Draw()
        {
            if (_style == null || _scale == null) return;

            DrawWedges();
            DrawRings();
            DrawCoreGlow();
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
    }
}

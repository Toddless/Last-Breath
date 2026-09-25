namespace Core.PassiveTree.View
{
    using System;

    /// <summary>
    /// Converts document coordinates to screen pixels and back. Two multipliers: <see cref="Spread"/>
    /// stretches distances only (nodes keep their authored size), <see cref="Zoom"/> scales everything on
    /// screen including node size. A position uses both (<see cref="PositionScale"/>), a size only the
    /// zoom. Neither multiplier reaches the document — writes go through
    /// <see cref="DocumentX"/>/<see cref="DocumentY"/> so a drag lands where released at any spread.
    /// Deliberately Godot-free: this is the arithmetic behind hit-testing and must be readable on its own.
    /// </summary>
    public sealed class CanvasTransform : ICanvasScale
    {
        /// <summary>Bounds on-screen node size; spread is excluded so a node is the same pixel size at
        /// every spread.</summary>
        public const float MinZoom = 0.08f;

        public const float MaxZoom = 4f;
        public const float ZoomStep = 1.15f;

        /// <summary>1 = the authored layout. Range wide enough to separate overlapping nodes yet keep a
        /// ray on screen at a usable zoom.</summary>
        public const float DefaultSpread = 1f;

        public const float MinSpread = 0.5f;
        public const float MaxSpread = 5f;

        /// <summary>Step of the control/key press; a quarter of the default, so stepping down from
        /// anywhere lands back on 1.</summary>
        public const float SpreadStep = 0.25f;

        /// <summary>Half the smallest stored step — below this two spread values are the same grid
        /// stop.</summary>
        private const float Epsilon = SpreadStep / 100f;

        private float _zoom = 1f;
        private float _spread = DefaultSpread;

        public float Zoom => _zoom;

        public float Spread => _spread;

        public float PanX { get; private set; }

        public float PanY { get; private set; }

        /// <summary>Document units to screen pixels for anything placed by a node's coordinates.</summary>
        public float PositionScale => _zoom * _spread;

        public float ScreenX(float documentX) => documentX * PositionScale + PanX;

        public float ScreenY(float documentY) => documentY * PositionScale + PanY;

        public float DocumentX(float screenX) => (screenX - PanX) / PositionScale;

        public float DocumentY(float screenY) => (screenY - PanY) / PositionScale;

        /// <summary>A screen length in document units — e.g. what picking slack is worth against the
        /// spatial grid.</summary>
        public float DocumentLength(float screenLength) => screenLength / PositionScale;

        /// <summary>Clamps and snaps a spread to its grid. Used both on read and write so a file and the
        /// view can never drift apart (e.g. file keeps 2.3 while the view shows 2.25).</summary>
        public static float NormalizeSpread(float value) =>
            Clamp(MathF.Round(value / SpreadStep) * SpreadStep, MinSpread, MaxSpread);

        /// <summary>
        /// The drawing frame's origin and scale, bundled as one value. Scale is
        /// <see cref="PositionScale"/> and not the zoom, since the frame carries positions — scaling by
        /// zoom alone would draw at one spread and pick at another. Handed out as a struct so a headless
        /// test can aim at exactly where the frame (which lives in an engine node) put the node.
        /// </summary>
        public CanvasFrame Frame() => new(PanX, PanY, PositionScale);

        public void SetPan(float x, float y)
        {
            PanX = x;
            PanY = y;
        }

        public void MovePan(float deltaX, float deltaY) => SetPan(PanX + deltaX, PanY + deltaY);

        /// <summary>Zooms about a screen point: the document under that pixel stays under it (magnifier,
        /// not jump).</summary>
        public void ZoomBy(float factor, float pivotScreenX, float pivotScreenY)
        {
            float pivotX = DocumentX(pivotScreenX);
            float pivotY = DocumentY(pivotScreenY);

            _zoom = Clamp(_zoom * factor, MinZoom, MaxZoom);
            Anchor(pivotX, pivotY, pivotScreenX, pivotScreenY);
        }

        /// <summary>Raises zoom to at least <paramref name="floor"/>, never lowers it — arriving at a
        /// node should not undo an existing close-up.</summary>
        public void RaiseZoomTo(float floor)
        {
            if (_zoom < floor) _zoom = Clamp(floor, MinZoom, MaxZoom);
        }

        /// <summary>Sets the spread (snapped, clamped), keeping the document point at the given screen
        /// position fixed. Returns whether anything changed, so a caller mirroring the value in a control
        /// isn't told a no-op press changed it.</summary>
        public bool SetSpread(float value, float anchorScreenX, float anchorScreenY)
        {
            float snapped = NormalizeSpread(value);
            if (MathF.Abs(snapped - _spread) < Epsilon) return false;

            float anchorX = DocumentX(anchorScreenX);
            float anchorY = DocumentY(anchorScreenY);

            _spread = snapped;
            Anchor(anchorX, anchorY, anchorScreenX, anchorScreenY);
            return true;
        }

        public bool NudgeSpread(int steps, float anchorScreenX, float anchorScreenY) =>
            SetSpread(_spread + steps * SpreadStep, anchorScreenX, anchorScreenY);

        /// <summary>Puts one document point in the middle of a view of the given size.</summary>
        public void CenterOn(float documentX, float documentY, float viewWidth, float viewHeight) =>
            Anchor(documentX, documentY, viewWidth * 0.5f, viewHeight * 0.5f);

        /// <summary>Fits a document-space box into the view with padding, touching only zoom — spread is
        /// a reading setting its owner chose and framing must not override it.</summary>
        public void Fit(float minX, float minY, float maxX, float maxY, float viewWidth, float viewHeight, float padding)
        {
            float extentX = MathF.Max(maxX - minX, 1f) * _spread;
            float extentY = MathF.Max(maxY - minY, 1f) * _spread;
            float availableX = MathF.Max(viewWidth - padding, 1f);
            float availableY = MathF.Max(viewHeight - padding, 1f);

            _zoom = Clamp(MathF.Min(availableX / extentX, availableY / extentY), MinZoom, MaxZoom);
            CenterOn((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, viewWidth, viewHeight);
        }

        /// <summary>Puts a view back where it was read from, zoom and pan together. The zoom passes the
        /// same bounds a wheel click does, so a stored value can never open the tree at a scale the
        /// controls cannot reach; the spread is left alone, being the document's and not the reader's.</summary>
        public void Restore(float zoom, float panX, float panY)
        {
            _zoom = Clamp(zoom, MinZoom, MaxZoom);
            SetPan(panX, panY);
        }

        /// <summary>Resets zoom to 1, keeping spread for the same reason as <see cref="Fit"/>.</summary>
        public void ResetZoom(float viewWidth, float viewHeight)
        {
            _zoom = 1f;
            SetPan(viewWidth * 0.5f, viewHeight * 0.5f);
        }

        /// <summary>The one move behind zoom-about-cursor, spread-about-middle and center-on-node: pans
        /// so a document point sits at a screen position.</summary>
        private void Anchor(float documentX, float documentY, float screenX, float screenY) =>
            SetPan(screenX - documentX * PositionScale, screenY - documentY * PositionScale);

        private static float Clamp(float value, float min, float max) =>
            value < min ? min : value > max ? max : value;
    }
}

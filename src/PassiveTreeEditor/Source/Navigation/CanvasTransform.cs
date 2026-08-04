namespace PassiveTreeEditor.Source.Navigation
{
    using System;

    /// <summary>
    /// The single place a document coordinate becomes a screen pixel and a screen pixel becomes a
    /// document coordinate.
    /// <para>Two multipliers sit between the two spaces and they do not mean the same thing.
    /// <see cref="Spread"/> stretches distances and nothing else: the gap between two nodes grows while
    /// each node keeps the size it was authored at, which is what makes a crowded wheel readable
    /// instead of merely bigger. <see cref="Zoom"/> scales what is on screen, nodes included. A
    /// position therefore travels through both (<see cref="PositionScale"/>), a size only through the
    /// zoom.</para>
    /// <para>Neither multiplier ever reaches the document. A coordinate written back into a node comes
    /// from <see cref="DocumentX"/>/<see cref="DocumentY"/>, so a drag lands where the hand released it
    /// at any spread, and the file keeps the layout its author typed.</para>
    /// <para>Deliberately free of Godot: this is the arithmetic behind every click that has to hit what
    /// the eye aimed at, and a mismatch between drawing and picking is invisible until someone misses.
    /// It has to be readable on its own.</para>
    /// </summary>
    public sealed class CanvasTransform
    {
        /// <summary>Zoom bounds are bounds on how big a node may get on screen, so the spread does not
        /// enter them: a node is the same number of pixels across at every spread by design.</summary>
        public const float MinZoom = 0.08f;

        public const float MaxZoom = 4f;
        public const float ZoomStep = 1.15f;

        /// <summary>The authored layout is 1. Below it the wheel is packed tighter than the file says,
        /// above it the gaps open up; the range is wide enough to tell apart two nodes that overlap at
        /// 1 and still short of the point where a ray no longer fits on screen at a usable zoom.</summary>
        public const float DefaultSpread = 1f;

        public const float MinSpread = 0.5f;
        public const float MaxSpread = 5f;

        /// <summary>Step of the control and of one key press. A quarter divides the default exactly, so
        /// stepping down from anywhere lands back on 1 rather than near it.</summary>
        public const float SpreadStep = 0.25f;

        /// <summary>Half a step of the smallest thing that is stored, which is the spread grid: below
        /// this two values are the same stop and setting one over the other is not a change.</summary>
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

        /// <summary>A length measured on screen, in the document's own units — what a few pixels of
        /// picking slack are worth to a query against the spatial grid.</summary>
        public float DocumentLength(float screenLength) => screenLength / PositionScale;

        public void SetPan(float x, float y)
        {
            PanX = x;
            PanY = y;
        }

        public void MovePan(float deltaX, float deltaY) => SetPan(PanX + deltaX, PanY + deltaY);

        /// <summary>Zooms about a point on screen: the document under that pixel stays under it, which
        /// is what makes the wheel a magnifier rather than a jump.</summary>
        public void ZoomBy(float factor, float pivotScreenX, float pivotScreenY)
        {
            float pivotX = DocumentX(pivotScreenX);
            float pivotY = DocumentY(pivotScreenY);

            _zoom = Clamp(_zoom * factor, MinZoom, MaxZoom);
            Anchor(pivotX, pivotY, pivotScreenX, pivotScreenY);
        }

        /// <summary>Raises the zoom to at least <paramref name="floor"/> and never lowers it. Arriving
        /// from the whole-tree view onto a node two pixels across is not arriving anywhere, while a jump
        /// that pulled the view out would throw away a close-up someone was working in.</summary>
        public void RaiseZoomTo(float floor)
        {
            if (_zoom < floor) _zoom = Clamp(floor, MinZoom, MaxZoom);
        }

        /// <summary>
        /// Sets the spread, snapped to its own step and clamped to its bounds, keeping the document
        /// point at the given screen position where it is. Answers whether anything moved, so a caller
        /// that mirrors the value in a control is not told about a press that changed nothing.
        /// </summary>
        public bool SetSpread(float value, float anchorScreenX, float anchorScreenY)
        {
            float snapped = Clamp(MathF.Round(value / SpreadStep) * SpreadStep, MinSpread, MaxSpread);
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

        /// <summary>
        /// Fits a document-space box into a view, leaving <paramref name="padding"/> pixels of margin in
        /// total on each axis. Only the zoom is touched: the spread is a reading setting its owner chose
        /// and framing has no business undoing it, so a spread-out tree frames spread out.
        /// </summary>
        public void Fit(float minX, float minY, float maxX, float maxY, float viewWidth, float viewHeight, float padding)
        {
            float extentX = MathF.Max(maxX - minX, 1f) * _spread;
            float extentY = MathF.Max(maxY - minY, 1f) * _spread;
            float availableX = MathF.Max(viewWidth - padding, 1f);
            float availableY = MathF.Max(viewHeight - padding, 1f);

            _zoom = Clamp(MathF.Min(availableX / extentX, availableY / extentY), MinZoom, MaxZoom);
            CenterOn((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, viewWidth, viewHeight);
        }

        /// <summary>Resets the view to the untouched state, keeping the spread for the same reason
        /// <see cref="Fit"/> does.</summary>
        public void ResetZoom(float viewWidth, float viewHeight)
        {
            _zoom = 1f;
            SetPan(viewWidth * 0.5f, viewHeight * 0.5f);
        }

        /// <summary>Pans so that a document point sits at a screen position — the one move behind
        /// zooming about the cursor, spreading about the middle and centring on a node.</summary>
        private void Anchor(float documentX, float documentY, float screenX, float screenY) =>
            SetPan(screenX - documentX * PositionScale, screenY - documentY * PositionScale);

        private static float Clamp(float value, float min, float max) =>
            value < min ? min : value > max ? max : value;
    }
}

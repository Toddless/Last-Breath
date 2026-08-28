namespace Core.Entity.Silhouette
{
    /// <summary>
    /// The rectangle of non-transparent pixels of one animation frame, in frame pixels (the
    /// convention frame is 480x480 with the sprite centered on it). This is what
    /// Image.GetUsedRect() reports for the frame, carried into Godot-free code as plain floats.
    /// </summary>
    public readonly record struct SilhouetteRect(float X, float Y, float Width, float Height)
    {
        /// <summary>Lowest opaque row — where the feet touch the ground in frame pixels.</summary>
        public float Bottom => Y + Height;

        /// <summary>Horizontal middle of the body in frame pixels.</summary>
        public float CenterX => X + Width / 2f;

        /// <summary>A fully transparent frame (or a failed measurement) produces a degenerate
        /// rect; proportions from it would be division by zero, so callers fall back.</summary>
        public bool IsUsable => Width > 0 && Height > 0;
    }
}

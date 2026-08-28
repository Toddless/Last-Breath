namespace Core.World.DualGrid
{
    /// <summary>
    /// The knobs a scatter reads: how many props a painted cell grows, how far from the cell border their roots
    /// may sit, and how much the copies are allowed to differ. One profile serves the whole layer — per-terrain
    /// profiles are a later stage and deliberately have no seam here yet.
    /// </summary>
    public sealed record PropScatterSettings
    {
        /// <summary>Side of a world cell in pixels; offsets are measured inside it.</summary>
        public float CellSize { get; init; } = 128f;

        /// <summary>Layer-wide seed. Two layers with different seeds scatter differently over the same cells.</summary>
        public int Seed { get; init; }

        public int MinPropsPerCell { get; init; } = 2;

        public int MaxPropsPerCell { get; init; } = 4;

        /// <summary>Keep-out band along the cell border, so props never root on the seam between two cells.</summary>
        public float EdgeMargin { get; init; } = 24f;

        public float MinScale { get; init; } = 0.9f;

        public float MaxScale { get; init; } = 1.1f;

        /// <summary>Half-width of the brightness shift: a placement's tint lands in [-value, +value].</summary>
        public float TintJitter { get; init; } = 0.08f;
    }
}

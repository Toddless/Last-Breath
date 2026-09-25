namespace Core.World.DualGrid
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Grows the props of a single painted world cell. The answer depends on nothing but the cell address and
    /// the settings, so the same meadow comes back identical after a reload, and a cell repainted at the far
    /// end of a session grows exactly what it grew before.
    /// </summary>
    /// <remarks>
    /// The randomness is a 32-bit integer pipeline on purpose. Folding coordinates through floating point
    /// degenerates the noise — neighbouring cells collapse onto the same value once the products stop being
    /// exactly representable — so every step here stays in <see cref="uint"/> under <c>unchecked</c>, where
    /// wrap-around is the intended arithmetic rather than a lost bit.
    /// </remarks>
    public static class PropScatter
    {
        private static readonly IReadOnlyList<PropPlacement> Nothing = [];

        /// <summary>Every prop the cell grows, in a stable order — index 0 is always the same prop.</summary>
        public static IReadOnlyList<PropPlacement> PlacementsFor(GridCoordinate cell, PropScatterSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            int minimum = Math.Max(0, settings.MinPropsPerCell);
            int maximum = Math.Max(minimum, settings.MaxPropsPerCell);
            if (maximum == 0) return Nothing;

            // The margin can be asked to eat more than the cell holds; then every root sits on the centre line.
            float margin = Math.Clamp(settings.EdgeMargin, 0f, settings.CellSize / 2f);
            float span = settings.CellSize - (margin * 2f);

            float minimumScale = settings.MinScale;
            float maximumScale = Math.Max(minimumScale, settings.MaxScale);
            float tint = Math.Abs(settings.TintJitter);

            Stream stream = new(SeedOf(cell, settings.Seed));
            int count = stream.NextCount(minimum, maximum);
            PropPlacement[] placements = new PropPlacement[count];

            for (int index = 0; index < count; index++)
            {
                // Every draw happens for every prop, whatever the settings, so one knob never shifts another's
                // numbers: the stream position is part of the contract these pins freeze.
                float offsetX = margin + (stream.NextUnit() * span);
                float offsetY = margin + (stream.NextUnit() * span);
                bool mirrored = stream.NextBit();
                float scale = minimumScale + (stream.NextUnit() * (maximumScale - minimumScale));
                float tintShift = -tint + (stream.NextUnit() * (tint * 2f));

                placements[index] = new PropPlacement(offsetX, offsetY, mirrored, scale, tintShift);
            }

            return placements;
        }

        /// <summary>
        /// Cell address and layer seed folded into one starting state. Each part goes through the avalanche
        /// separately, so moving one cell across changes every bit rather than the low ones.
        /// </summary>
        private static uint SeedOf(GridCoordinate cell, int seed)
        {
            unchecked
            {
                uint state = Avalanche((uint)seed ^ 0x9E3779B9u);
                state = Avalanche(state ^ ((uint)cell.X * 0x85EBCA6Bu));
                state = Avalanche(state ^ ((uint)cell.Y * 0xC2B2AE35u));

                return state;
            }
        }

        /// <summary>Murmur3's 32-bit finaliser: pure bit mixing, no state, every input bit reaches every output bit.</summary>
        private static uint Avalanche(uint value)
        {
            unchecked
            {
                value ^= value >> 16;
                value *= 0x85EBCA6Bu;
                value ^= value >> 13;
                value *= 0xC2B2AE35u;
                value ^= value >> 16;

                return value;
            }
        }

        /// <summary>xorshift32 — a handful of shifts, identical on every machine, and never a Random in sight.</summary>
        private struct Stream(uint seed)
        {
            // xorshift32 is a fixed point at zero; the constant is an arbitrary non-zero start.
            private uint _state = seed == 0u ? 0x6D2B79F5u : seed;

            public uint Next()
            {
                unchecked
                {
                    _state ^= _state << 13;
                    _state ^= _state >> 17;
                    _state ^= _state << 5;

                    return _state;
                }
            }

            /// <summary>A value in [0, 1). The division is exact, so the float is the same on any runtime.</summary>
            public float NextUnit() => (float)(Next() * (1.0 / 4294967296.0));

            public bool NextBit() => (Next() & 1u) == 1u;

            public int NextCount(int minimum, int maximum) =>
                minimum + (int)(Next() % (uint)(maximum - minimum + 1));
        }
    }
}

namespace Core.Battle
{
    using System;
    using Entity.Components;

    /// <summary>
    /// How far an augment's numbers may fall from the figure its record declares (see CombatRules.json,
    /// "abilityAugments" section). The spread is one number for every property of every augment there
    /// is: a record carries the base and says nothing about the roll, so widening or closing the band
    /// is one edit instead of a hundred and thirty-four.
    /// </summary>
    /// <param name="ValueSpread">Half-width of the band, as a share of the base: 0.25 lets a value land
    /// anywhere between three quarters and five quarters of what the record declares.</param>
    public record AugmentValueRules(float ValueSpread)
    {
        /// <summary>How close to whole a base has to be to count as a count.</summary>
        private const float WholeTolerance = 0.0001f;

        /// <summary>The unit a count is counted in, and so the least a count can be worth and still be
        /// one at all.</summary>
        private const float SmallestCount = 1f;

        /// <summary>The least of its base a value keeps at the very bottom of the band. Only a spread
        /// wide enough to take the whole of the base away reaches this far down.</summary>
        private const float SmallestShareOfBase = 0.01f;

        /// <summary>No spread at all — what a composition holding no rules file rolls. Every instance
        /// comes out at its base, which is what an augment was worth before there were instances.</summary>
        public static readonly AugmentValueRules Fixed = new(0f);

        /// <summary>The bottom edge of the band, as a share of the base. A spread of one takes the
        /// whole of the base away and anything wider reaches past it, so the edge stops short of
        /// nothing instead of following the spread down: a value drawn to the other side of zero is an
        /// augment doing the opposite of what its record says, and the spread is a balance figure that
        /// moves while that guarantee does not.</summary>
        private float LowestShare => MathF.Max(1f - ValueSpread, SmallestShareOfBase);

        private float HighestShare => 1f + ValueSpread;

        public bool Rolls => ValueSpread > 0f;

        /// <summary>What one property of one instance is worth. Rolled once, when the instance is
        /// minted: a value recomputed on every read would move between two looks at the same augment.
        /// A spread of nothing never touches the generator — a band of zero width has nothing to draw,
        /// and spending a draw on it would shift every seeded sequence behind it.</summary>
        public float Roll(float baseValue, IRandomNumberGenerator rnd) =>
            Rolls ? Land(baseValue, baseValue * rnd.RandFloatRange(LowestShare, HighestShare)) : baseValue;

        /// <summary>Where a rolled value lands. A base written as a whole number is a count — turns,
        /// attacks, stacks — and a count stays whole, because the number the description prints and the
        /// number the augment applies have to be the same one: left fractional, it would be truncated
        /// by whoever consumes it and the tooltip would go on advertising the fraction.
        /// A count never lands on nothing, however wide the band is opened: an augment worth zero is
        /// chosen, worn, paid for and does nothing at all, which is the silent refusal the whole socket
        /// line exists to rule out. The floor sits under the landing rather than under the band, so it
        /// is the rounding itself that cannot reach nothing — one is the smallest a count comes out at
        /// whatever width the balance gives the spread. A base of nothing stays nothing: the floor keeps
        /// a count from being lost to the rounding, it does not mint one no record declared.</summary>
        private static float Land(float baseValue, float rolled)
        {
            if (!IsWhole(baseValue)) return rolled;

            float landed = MathF.Round(rolled, MidpointRounding.AwayFromZero);
            return MathF.Abs(landed) < SmallestCount ? SmallestCount * MathF.Sign(baseValue) : landed;
        }

        private static bool IsWhole(float value) => MathF.Abs(value - MathF.Round(value)) < WholeTolerance;
    }
}

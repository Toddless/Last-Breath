namespace Core
{
    using System;
    using Entity.Components;
    using Enums;
    using Godot;

    /// <summary>
    /// The one place a combat chance becomes a verdict: everything a fighter can succeed or fail at —
    /// evasion, block, suppression, crit, an extra swing, a counter — asks here, so the comparison rule
    /// is written once and luck has a single seam to work through.
    /// </summary>
    public static class ChanceRoll
    {
        /// <summary>Rolls <paramref name="chance"/> (0..1) on the caller's stream.</summary>
        public static bool Roll(float chance, IRandomNumberGenerator rnd, ChanceLuck luck = ChanceLuck.Neutral)
            => Roll(chance, rnd.RandFloat, luck);

        /// <summary>Rolls <paramref name="chance"/> (0..1) on a Godot stream — the one attacks carry.</summary>
        public static bool Roll(float chance, RandomNumberGenerator rnd, ChanceLuck luck = ChanceLuck.Neutral)
            => Roll(chance, rnd.Randf, luck);

        /// <summary>
        /// The rule itself, over a bare draw — also the only shape a stream nobody can build outside the
        /// engine (an attack's own generator) can be rolled through. A lucky roll draws twice and keeps the
        /// lower draw, an unlucky one the higher. The draw is taken whatever the chance, so a caller that
        /// must not burn a roll gates before asking (see <see cref="Calculations"/>' suppression).
        /// </summary>
        public static bool Roll(float chance, Func<float> draw, ChanceLuck luck = ChanceLuck.Neutral)
        {
            float roll = draw();
            if (luck is not ChanceLuck.Neutral)
            {
                float second = draw();
                roll = luck is ChanceLuck.Lucky ? Math.Min(roll, second) : Math.Max(roll, second);
            }

            return chance > 0 && roll <= chance;
        }
    }
}

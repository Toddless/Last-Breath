namespace Core.Battle.Abilities
{
    using System;
    using System.Collections.Generic;
    using Enums;

    /// <summary>
    /// What a record's design line means when it states a RANGE. "10–45% at Uncommon–Legendary" is not
    /// a band a copy draws from freely: it is a ladder with a rung per rarity of the record's band, and
    /// a copy's rarity picks the rung — and picks it EXACTLY. Nothing is drawn around it: a copy is its
    /// record plus its rarity, so a Legendary copy is worth more than an Uncommon one of the same record
    /// and two copies of one record at one rarity are the same augment.
    ///
    /// Two roads to a rung. Where the author wrote the range's two ends, the ends are handed back
    /// untouched and the steps between them are interpolated evenly and rounded, because a ladder
    /// written as 10 / 21.67 / 33.33 / 45 reads as arithmetic rather than as design. Where the even
    /// steps read badly — a narrow share ladder rounds two rarities onto one figure — the author writes
    /// every rung out instead, and that list is used as it stands.
    /// </summary>
    public static class AugmentValueLadder
    {
        /// <summary>What a rounded rung lands on when the ends are shares — fives in the percent the
        /// player is shown, which is a twentieth of the share the data carries.</summary>
        private const float ShareStep = 0.05f;

        /// <summary>What a rounded rung lands on when the ends are counts. Attacks, projectiles and
        /// turns are counted in ones, and rounding those to fives would collapse a ladder of 1–3 into
        /// a single rung.</summary>
        private const float CountStep = 1f;

        private const float WholeTolerance = 0.0001f;

        /// <summary>
        /// The rung a copy of this rarity stands on. <paramref name="atWorst"/> is what the design line
        /// gives the worst rarity of the band and <paramref name="atBest"/> what it gives the best; a
        /// record whose line names one number passes the same figure twice and the ladder answers it
        /// for every rarity, which is a constant and costs nothing to ask for.
        /// </summary>
        /// <param name="rarity">The copy's own rarity. Outside the band it is clamped to the nearer
        /// end: a table may seat a copy at a rarity the record never rolls, and the rung it gets is the
        /// closest the record actually describes rather than an extrapolation nobody authored.</param>
        public static float Rung(float atWorst, float atBest, (Rarity Worst, Rarity Best) band, Rarity rarity)
        {
            // A record naming one number has no rungs to invent, and rounding the figure it DOES name to
            // the grid would quietly move it — a 2% burn is not 0% because five is the step of a ladder
            // it never had.
            if (!IsLadder(atWorst, atBest)) return atWorst;

            // The scale runs downward — Legendary is zero — so the worst end is the larger number.
            int worst = (int)band.Worst;
            int best = (int)band.Best;
            if (worst <= best) return atBest;

            int steps = worst - best;
            int fromWorst = Math.Clamp(worst - (int)rarity, 0, steps);
            if (fromWorst == 0) return atWorst;
            if (fromWorst == steps) return atBest;

            float raw = atWorst + ((atBest - atWorst) * fromWorst / steps);
            float rounded = Round(raw, StepOf(atWorst, atBest));

            // The grid may be coarser than the range is wide. Rounding is there to make an invented rung
            // read as design, never to push it past an end the author wrote.
            return Math.Clamp(rounded, MathF.Min(atWorst, atBest), MathF.Max(atWorst, atBest));
        }

        /// <summary>
        /// The rung a copy of this rarity stands on when the author wrote every step out. Nothing is
        /// interpolated and nothing is rounded: the list IS the ladder, worst rarity first. A rarity
        /// outside the band takes the nearest end, exactly as the interpolated path does.
        /// </summary>
        public static float Rung(IReadOnlyList<float> authored, (Rarity Worst, Rarity Best) band, Rarity rarity)
        {
            int worst = (int)band.Worst;
            int steps = Math.Max(0, worst - (int)band.Best);
            int fromWorst = Math.Clamp(worst - (int)rarity, 0, steps);
            return authored[Math.Clamp(fromWorst, 0, authored.Count - 1)];
        }

        /// <summary>How many rungs a band of this width has — what an authored list has to be as long as.</summary>
        public static int RungCount((Rarity Worst, Rarity Best) band) => Math.Max(0, (int)band.Worst - (int)band.Best) + 1;

        /// <summary>Whether the two ends describe a ladder at all — one number is a constant, and a
        /// record that names one is not carrying a range the rounding could mangle.</summary>
        public static bool IsLadder(float atWorst, float atBest) => Math.Abs(atWorst - atBest) > WholeTolerance;

        /// <summary>Counts step by one, everything else by a twentieth. Read off the ENDS rather than
        /// off the interpolated figure: what unit a record is written in is the author's, and an
        /// interpolation landing near a whole number does not make a share into a count.</summary>
        private static float StepOf(float atWorst, float atBest) =>
            IsWhole(atWorst) && IsWhole(atBest) ? CountStep : ShareStep;

        private static float Round(float value, float step) =>
            MathF.Round(value / step, MidpointRounding.AwayFromZero) * step;

        private static bool IsWhole(float value) => MathF.Abs(value - MathF.Round(value)) < WholeTolerance;
    }
}

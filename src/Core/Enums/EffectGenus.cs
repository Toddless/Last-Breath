namespace Core.Enums
{
    /// <summary>
    /// What kind of thing an effect IS, decided on the effect itself rather than on the record that laid
    /// it: items, passive skills and boss stages lay effects with no record at all, so anything reading a
    /// cast's declaration would be blind to half the effects in a fight.
    /// <para>The three are exhaustive and mutually exclusive, which is what lets a knob per kind and a
    /// knob over all effects say the same thing about every effect in the game.</para>
    /// </summary>
    public enum EffectGenus
    {
        /// <summary>Neither flagged harmful nor ticking damage. What nothing classifies lands here, the
        /// same default the buff/debuff counters have always read.</summary>
        Buff,

        /// <summary>Flagged harmful and ticking no damage: the controls, the shreds, the curses.</summary>
        Debuff,

        /// <summary>Ticks damage over turns. Counted apart from the debuffs because what a damage build
        /// buys is the tick, and because a damaging effect nobody flagged harmful is still not a gift.</summary>
        Damaging
    }
}

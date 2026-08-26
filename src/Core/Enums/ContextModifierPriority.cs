namespace Core.Enums
{
    /// <summary>
    /// Application order of <see cref="Context.IContextModifier{T}"/> in a pipeline
    /// (ModifierHandlerComponent sorts ascending — lower runs first). A SEMANTIC scale, not a
    /// counter: the wide gaps are deliberate slack for future in-between steps without renumbering.
    /// Distinct from <see cref="Priority"/>, which orders parameter/ability DECORATORS.
    /// </summary>
    public enum ContextModifierPriority
    {
        /// <summary>Runs before the bulk: context setup, base overrides.</summary>
        Early = -100,

        /// <summary>What the character is rather than what he carries: the passive tree hands a pipeline
        /// one modifier per knob, holding the total of every taken node that feeds it. Ahead of
        /// <see cref="Normal"/>, so the build is the ground the rest of a pipeline works on and its order
        /// against everything sitting at <see cref="Normal"/> is fixed instead of incidental; behind
        /// <see cref="Early"/>, which still owns context setup.</summary>
        Innate = -50,

        /// <summary>The default home of regular additive/multiplicative tweaks.</summary>
        Normal = 0,

        /// <summary>Runs after the bulk: reads what Normal produced (e.g. "always crit" flags).</summary>
        Late = 100,

        /// <summary>The last word on a NUMBER. Damage CONVERSIONS ("X% of physical dealt as fire") belong
        /// here: a conversion must see the final number, so nothing that shapes one may run after it.
        /// Also home of hard rule overrides (unblockable/unevadable).</summary>
        Absolute = 1000,

        /// <summary>Past the last word: an OUTGOING rule that ENDS a damage component instead of shaping it
        /// ("attacks deal no physical damage"), and so a step of the SOURCE's lists. Every rule entitled to a
        /// share of a component takes it at <see cref="Absolute"/> or earlier, so what a conversion has
        /// already carried into another type survives and only the untouched remainder is denied. A denial
        /// may not share the conversions' slot: ties there are ordered by nothing.
        /// <para>An incoming immunity of the RECEIVER zeroes components too and still belongs at
        /// <see cref="Absolute"/>: it empties the whole dictionary in a pass of its own, where there is
        /// nobody left to argue with about shares.</para></summary>
        Denial = 2000,
    }
}

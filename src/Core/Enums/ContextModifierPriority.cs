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

        /// <summary>The default home of regular additive/multiplicative tweaks.</summary>
        Normal = 0,

        /// <summary>Runs after the bulk: reads what Normal produced (e.g. "always crit" flags).</summary>
        Late = 100,

        /// <summary>The very last word. Damage CONVERSIONS ("X% of physical dealt as fire") belong
        /// here when they arrive: a conversion must see the final number, so nothing may run after it.
        /// Also home of hard rule overrides (unblockable/unevadable).</summary>
        Absolute = 1000,
    }
}

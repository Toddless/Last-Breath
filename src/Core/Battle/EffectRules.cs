namespace Core.Battle
{
    /// <summary>
    /// Parsed effect rules (see CombatRules.json, "effects" section). What every effect instance
    /// there is shares, regardless of who applied it or what it does.
    /// </summary>
    /// <param name="MaxExtendedTurns">Total turns ONE instance may ever gain from extensions, however
    /// many extenders reach it. The number is a budget rather than a per-source limit: extension is
    /// worth buying as long as an effect is short, and a cap counted per source would let a second
    /// extender restart the count and keep a debuff standing for the rest of the fight.</param>
    public record EffectRules(int MaxExtendedTurns)
    {
        /// <summary>What an effect works at when it cannot reach the rules: a host composing no rules
        /// provider (tests, tools) still caps extensions, because an unreachable file must not turn
        /// the budget off — an effect without a cap is the one outcome the budget exists to rule out.
        /// The figure is a balance placeholder and lives in the shipped file; this one only has to be
        /// a working cap.</summary>
        public static readonly EffectRules Default = new(3);
    }
}

namespace LastBreathTest.BattleSystemTests
{
    using Core.Battle;

    /// <summary>The combat rules a walk pins by hand: everything but the band it is about left at
    /// nothing, so what a case measures is written in the case rather than in a shipped file that a
    /// balance pass is free to move.</summary>
    internal sealed class StubCombatRules(AugmentValueRules values) : ICombatRulesProvider
    {
        public ControlResistanceRules ControlResistance => ControlResistanceRules.Disabled;

        public ArenaRules Arena => ArenaRules.Default;

        public ExhaustionRules Exhaustion => ExhaustionRules.Disabled;

        public AugmentValueRules AugmentValues { get; } = values;

        public EffectRules Effects => EffectRules.Default;
    }
}

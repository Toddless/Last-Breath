namespace LastBreathTest.BattleSystemTests
{
    using Core.Battle;

    /// <summary>The combat rules a walk pins by hand: everything left at nothing, so what a case
    /// measures is written in the case rather than in a shipped file a balance pass is free to move.</summary>
    internal sealed class StubCombatRules : ICombatRulesProvider
    {
        public ControlResistanceRules ControlResistance => ControlResistanceRules.Disabled;

        public ArenaRules Arena => ArenaRules.Default;

        public ExhaustionRules Exhaustion => ExhaustionRules.Disabled;

        public EffectRules Effects => EffectRules.Default;
    }
}

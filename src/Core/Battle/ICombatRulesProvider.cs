namespace Core.Battle
{
    /// <summary>Cross-cutting combat rules loaded from data (SharedData/CombatRules).</summary>
    public interface ICombatRulesProvider
    {
        ControlResistanceRules ControlResistance { get; }
        ArenaRules Arena { get; }
        ExhaustionRules Exhaustion { get; }
        AugmentValueRules AugmentValues { get; }
        EffectRules Effects { get; }
    }
}

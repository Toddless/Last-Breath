namespace Battle.Source.Effects
{
    using Core.Battle.Abilities;
    using Core.Enums;

    /// <summary>
    /// "Усиленная защита" — additively raises the bearer's critical-damage mitigation
    /// (reduces incoming critical damage). Additive across stacks.
    /// </summary>
    public class EnhanceDefenseEffect(int duration, int maxStacks, EffectValue value)
        : ParameterChangeEffect(
            id: "Effect_Enhance_Defense",
            duration,
            maxStacks,
            value,
            parameter: EntityParameter.CriticalDamageMitigation,
            type: OperationType.Add,
            priority: Priority.Weak,
            statusEffect: StatusEffects.None)
    {
        public override IEffect Copy() => new EnhanceDefenseEffect(Duration, MaxStacks, Authored);
    }
}

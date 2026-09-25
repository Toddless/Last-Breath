namespace Battle.Source.Effects
{
    using Core.Battle.Abilities;
    using Core.Enums;

    /// <summary>Deep Freeze stage-2 payload: reduces the target's cold resistance by <c>value</c>
    /// (0.25 = −25 percentage points) per stack.</summary>
    public class ColdResistanceShredEffect(int duration, int maxStacks, EffectValue value)
        : ParameterChangeEffect(id: "Effect_Cold_Resistance_Shred",
            duration,
            maxStacks,
            value,
            parameter: EntityParameter.ColdResistance,
            type: OperationType.Subtract,
            priority: Priority.Weak,
            statusEffect: StatusEffects.None)
    {
        public override bool IsHarmful => true;

        public override IEffect Copy() => new ColdResistanceShredEffect(Duration, MaxStacks, Authored);
    }
}

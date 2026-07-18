namespace Battle.Source.Effects
{
    using Core.Battle.Abilities;
    using Core.Enums;

    /// <summary>"Усталость": reduces the target's spell damage by <c>value</c> (0.15 = −15%) per stack.</summary>
    public class FatigueEffect(int duration, int maxStacks, float value)
        : ParameterChangeEffect(id: "Effect_Fatigue",
            duration,
            maxStacks,
            value: 1 - value,
            parameter: EntityParameter.SpellDamage,
            type: OperationType.Multiply,
            priority: Priority.Weak,
            statusEffect: StatusEffects.None)
    {
        // Copy takes the primary-ctor value, not the transformed base Value — re-inverting would flip it.
        public override IEffect Copy() => new FatigueEffect(Duration, MaxStacks, value);
    }
}

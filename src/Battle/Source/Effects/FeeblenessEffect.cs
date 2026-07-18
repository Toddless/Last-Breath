namespace Battle.Source.Effects
{
    using Core.Battle.Abilities;
    using Core.Enums;

    /// <summary>"Немощный": reduces the target's ATTACK damage by <c>value</c> (0.15 = −15%) per stack.
    /// The hit-damage counterpart is <see cref="Weakness"/>.</summary>
    public class FeeblenessEffect(int duration, int maxStacks, float value)
        : ParameterChangeEffect(id: "Effect_Feebleness",
            duration,
            maxStacks,
            value: 1 - value,
            parameter: EntityParameter.Damage,
            type: OperationType.Multiply,
            priority: Priority.Weak,
            statusEffect: StatusEffects.None)
    {
        // Copy takes the primary-ctor value, not the transformed base Value — re-inverting would flip it.
        public override IEffect Copy() => new FeeblenessEffect(Duration, MaxStacks, value);
    }
}

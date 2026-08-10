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
            value,
            parameter: EntityParameter.PhysicalDamage,
            type: OperationType.Multiply,
            priority: Priority.Weak,
            statusEffect: StatusEffects.None,
            shape: EffectValueShape.ShareLost)
    {
        public override bool IsHarmful => true;

        // Copy takes the authored share LOST, not the share left standing: the base scales before it
        // inverts, and a copy fed the inverted figure would invert it a second time.
        public override IEffect Copy() => new FeeblenessEffect(Duration, MaxStacks, value);
    }
}

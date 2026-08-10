namespace Battle.Source.Effects
{
    using Core.Battle.Abilities;
    using Core.Enums;

    /// <summary>"Гниение": reduces the target's health recovery by <c>value</c> (0.15 = −15%) per stack.</summary>
    public class RotEffect(int duration, int maxStacks, EffectValue value)
        : ParameterChangeEffect(id: "Effect_Rot",
            duration,
            maxStacks,
            value,
            parameter: EntityParameter.HealthRecovery,
            type: OperationType.Multiply,
            priority: Priority.Weak,
            statusEffect: StatusEffects.None,
            shape: EffectValueShape.ShareLost)
    {
        public override bool IsHarmful => true;

        // Copy takes the authored share LOST, not the share left standing: the base scales before it
        // inverts, and a copy fed the inverted figure would invert it a second time.
        public override IEffect Copy() => new RotEffect(Duration, MaxStacks, value);
    }
}

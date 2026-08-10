namespace Battle.Source.Effects
{
    using Core.Battle.Abilities;
    using Core.Enums;

    /// <summary>"Усталость": reduces the target's spell damage by <c>value</c> (0.15 = −15%) per stack.</summary>
    public class FatigueEffect(int duration, int maxStacks, float value)
        : ParameterChangeEffect(id: "Effect_Fatigue",
            duration,
            maxStacks,
            value,
            parameter: EntityParameter.SpellDamage,
            type: OperationType.Multiply,
            priority: Priority.Weak,
            statusEffect: StatusEffects.None,
            shape: EffectValueShape.ShareLost)
    {
        public override bool IsHarmful => true;

        // Copy takes the authored share LOST, not the share left standing: the base scales before it
        // inverts, and a copy fed the inverted figure would invert it a second time.
        public override IEffect Copy() => new FatigueEffect(Duration, MaxStacks, value);
    }
}

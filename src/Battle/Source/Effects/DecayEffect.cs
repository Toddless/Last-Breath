namespace Battle.Source.Effects
{
    using Core.Battle.Abilities;
    using Core.Enums;

    /// <summary>"Распад": reduces every recovery (health and mana) by <c>value</c> (0.15 = −15%) per stack.</summary>
    public class DecayEffect(int duration, int maxStacks, EffectValue value)
        : CompositeParameterChangeEffect(id: "Effect_Decay",
            duration,
            maxStacks,
            changes:
            [
                new ParameterChange(EntityParameter.HealthRecovery, value, OperationType.Multiply, Priority.Weak, EffectValueShape.ShareLost),
                new ParameterChange(EntityParameter.ManaRecovery, value, OperationType.Multiply, Priority.Weak, EffectValueShape.ShareLost)
            ])
    {
        public override bool IsHarmful => true;

        // Copy takes the primary-ctor value, not the transformed changes — re-inverting would flip them.
        public override IEffect Copy() => new DecayEffect(Duration, MaxStacks, value);
    }
}

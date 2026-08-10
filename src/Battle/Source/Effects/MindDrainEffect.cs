namespace Battle.Source.Effects
{
    using Core.Battle.Abilities;
    using Core.Enums;

    /// <summary>"Иссушение разума": reduces the target's mana recovery by <c>value</c> (0.15 = −15%) per stack.</summary>
    public class MindDrainEffect(int duration, int maxStacks, float value)
        : ParameterChangeEffect(id: "Effect_Mind_Drain",
            duration,
            maxStacks,
            value,
            parameter: EntityParameter.ManaRecovery,
            type: OperationType.Multiply,
            priority: Priority.Weak,
            statusEffect: StatusEffects.None,
            shape: EffectValueShape.ShareLost)
    {
        // Copy takes the authored share LOST, not the share left standing: the base scales before it
        // inverts, and a copy fed the inverted figure would invert it a second time.
        public override IEffect Copy() => new MindDrainEffect(Duration, MaxStacks, value);
    }
}

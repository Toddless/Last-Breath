namespace Battle.Source.Effects
{
    using Core.Battle.Abilities;
    using Core.Enums;

    public class BlindEffect(
        int duration,
        int maxStacks,
        float value)
        : ParameterChangeEffect(
            id: "Effect_Blind",
            duration,
            maxStacks,
            value,
            parameter: EntityParameter.Accuracy,
            type: OperationType.Multiply,
            priority: Priority.Weak,
            statusEffect: StatusEffects.Blind,
            shape: EffectValueShape.ShareLost)
    {
        public override bool IsHarmful => true;

        // Copy takes the authored share LOST, not the share left standing: the base scales before it
        // inverts, and a copy fed the inverted figure would invert it a second time.
        public override IEffect Copy() => new BlindEffect(Duration, MaxStacks, value);
    }
}

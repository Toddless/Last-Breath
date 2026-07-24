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
            value: 1 - value,
            parameter: EntityParameter.Accuracy,
            type: OperationType.Multiply,
            priority: Priority.Weak,
            statusEffect: StatusEffects.Blind)
    {
        public override bool IsHarmful => true;

        // Copy takes the primary-ctor value, not the transformed base Value — re-inverting would flip it.
        public override IEffect Copy() => new BlindEffect(Duration, MaxStacks, value);
    }
}

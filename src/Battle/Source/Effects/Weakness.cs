namespace Battle.Source.Effects
{
    using Core.Battle.Abilities;
    using Core.Enums;

    public class Weakness(
        int duration,
        int maxStacks,
        float value)
        : ParameterChangeEffect(
            id: "Effect_Weakness",
            duration,
            maxStacks,
            value: 1 - value,
            parameter: EntityParameter.Damage,
            type: OperationType.Multiply,
            priority: Priority.Weak,
            statusEffect: StatusEffects.None)

    {
        // Copy takes the primary-ctor value, not the transformed base Value — re-inverting would flip it.
        public override IEffect Copy() => new Weakness(Duration, MaxStacks, value);
    }
}

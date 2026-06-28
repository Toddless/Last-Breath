namespace Battle.Source.Effects
{
    using Core.Enums;
    using Core.Interfaces.Abilities;

    public class Weakness(
        int duration,
        int maxStacks,
        float value)
        : ParameterChangeEffect(
            id: "Effect_Weakness",
            duration,
            maxStacks,
            value,
            parameter: EntityParameter.Damage,
            type: OperationType.Multiply,
            priority: Priority.Weak,
            statusEffect: StatusEffects.None)

    {
        public override IEffect Copy() => new Weakness(Duration, MaxStacks, Value);
    }
}

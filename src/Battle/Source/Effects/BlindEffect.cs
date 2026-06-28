namespace Battle.Source.Effects
{
    using Core.Enums;
    using Core.Interfaces.Abilities;

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
            statusEffect: StatusEffects.Blind)
    {
        public override IEffect Copy() => new BlindEffect(Duration, MaxStacks, Value);
    }
}

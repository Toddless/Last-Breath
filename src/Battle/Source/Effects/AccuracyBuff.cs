namespace Battle.Source.Effects
{
    using Core.Enums;
    using Core.Interfaces.Abilities;

    /// <summary>
    /// Buff that increases the target's accuracy. Counterpart of <see cref="BlindEffect"/>.
    /// </summary>
    public class AccuracyBuff(
        int duration,
        int maxStacks,
        float value)
        : ParameterChangeEffect(
            id: "Effect_Accuracy_Buff",
            duration,
            maxStacks,
            value,
            parameter: EntityParameter.Accuracy,
            type: OperationType.Multiply,
            priority: Priority.Weak,
            statusEffect: StatusEffects.None)
    {
        public override IEffect Copy() => new AccuracyBuff(Duration, MaxStacks, Value);
    }
}

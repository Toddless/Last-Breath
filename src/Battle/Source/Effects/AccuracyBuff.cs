namespace Battle.Source.Effects
{
    using Core.Battle.Abilities;
    using Core.Enums;

    /// <summary>
    /// "Твёрдая рука": raises the bearer's accuracy by <c>value</c> (0.15 = +15%) per stack.
    /// Counterpart of <see cref="BlindEffect"/>.
    /// </summary>
    public class AccuracyBuff(
        int duration,
        int maxStacks,
        EffectValue value)
        : ParameterChangeEffect(
            id: "Effect_Accuracy_Buff",
            duration,
            maxStacks,
            value,
            parameter: EntityParameter.Accuracy,
            type: OperationType.Multiply,
            priority: Priority.Weak,
            statusEffect: StatusEffects.None,
            shape: EffectValueShape.ShareGained)
    {
        public override IEffect Copy() => new AccuracyBuff(Duration, MaxStacks, Authored);
    }
}

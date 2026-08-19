namespace Battle.Source.Effects
{
    using Core.Battle.Abilities;
    using Core.Enums;

    /// <summary>
    /// Debuff: reduces the target's armor by <c>reduceBy</c> (0..1) per stack.
    /// Stacks multiplicatively: n stacks => armor * (1 - reduceBy)^n.
    /// </summary>
    public class ArmorReductionEffect(int duration, int maxStacks, EffectValue reduceBy)
        : ParameterChangeEffect(id: "Effect_Armor_Reduction",
            duration,
            maxStacks,
            reduceBy,
            parameter: EntityParameter.Armor,
            type: OperationType.Multiply,
            priority: Priority.Weak,
            statusEffect: StatusEffects.None,
            shape: EffectValueShape.ShareLost)
    {
        public override bool IsHarmful => true;

        public override IEffect Copy() => new ArmorReductionEffect(Duration, MaxStacks, reduceBy);
    }
}

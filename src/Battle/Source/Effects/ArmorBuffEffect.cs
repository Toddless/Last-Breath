namespace Battle.Source.Effects
{
    using Core.Battle.Abilities;
    using Core.Enums;

    /// <summary>Buff: raises the bearer's armor by <c>value</c> (0..1) per stack, multiplicatively.</summary>
    public class ArmorBuffEffect(int duration, int maxStacks, float value)
        : ParameterChangeEffect(id: "Effect_Armor_Buff",
            duration,
            maxStacks,
            value,
            parameter: EntityParameter.Armor,
            type: OperationType.Multiply,
            priority: Priority.Weak,
            statusEffect: StatusEffects.None,
            shape: EffectValueShape.ShareGained)
    {
        // Copy takes the primary-ctor value, not the transformed base Value — re-transforming would double it.
        public override IEffect Copy() => new ArmorBuffEffect(Duration, MaxStacks, value);
    }
}

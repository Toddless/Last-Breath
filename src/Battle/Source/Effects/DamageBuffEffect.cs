namespace Battle.Source.Effects
{
    using Core.Battle.Abilities;
    using Core.Enums;

    /// <summary>Buff: increases the target's dealt damage by <c>value</c> (0..1) per stack, multiplicatively.</summary>
    public class DamageBuffEffect(int duration, int maxStacks, float value)
        : ParameterChangeEffect(id: "Effect_Damage_Buff",
            duration,
            maxStacks,
            value: 1 + value,
            parameter: EntityParameter.PhysicalDamage,
            type: OperationType.Multiply,
            priority: Priority.Weak,
            statusEffect: StatusEffects.None)
    {
        // Copy takes the primary-ctor value, not the transformed base Value — re-transforming would double it.
        public override IEffect Copy() => new DamageBuffEffect(Duration, MaxStacks, value);
    }
}

namespace Battle.Source.Effects
{
    using Core.Battle.Abilities;
    using Core.Enums;

    public class CriticalDamageBuffEffect(
        int duration,
        int maxStacks,
        float value)
        : ParameterChangeEffect(
            id: "Effect_Critical_Damage_Buff",
            duration,
            maxStacks,
            value,
            parameter: EntityParameter.CriticalDamage,
            type: OperationType.Add,
            priority: Priority.Weak,
            statusEffect: StatusEffects.None)
    {
        public override IEffect Copy() => new CriticalDamageBuffEffect(Duration, MaxStacks, Value);
    }
}

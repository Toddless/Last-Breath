namespace Battle.Source.Effects
{
    using Core.Enums;
    using Core.Interfaces.Abilities;

    /// <summary>
    /// Buff that increases the target's chance of an additional attack (AdditionalHitChance).
    /// </summary>
    public class AdditionalHitChanceEffect(
        int duration,
        int maxStacks,
        float value)
        : ParameterChangeEffect(
            id: "Effect_Additional_Hit_Chance_Buff",
            duration,
            maxStacks,
            value,
            parameter: EntityParameter.AdditionalHitChance,
            type: OperationType.Multiply,
            priority: Priority.Weak,
            statusEffect: StatusEffects.None)
    {
        public override IEffect Copy() => new AdditionalHitChanceEffect(Duration, MaxStacks, Value);
    }
}

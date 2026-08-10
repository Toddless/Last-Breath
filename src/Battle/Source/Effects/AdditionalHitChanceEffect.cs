namespace Battle.Source.Effects
{
    using Core.Battle.Abilities;
    using Core.Enums;

    /// <summary>
    /// Buff that increases the target's chance of an additional attack (AdditionalHitChance).
    /// </summary>
    public class AdditionalHitChanceEffect(
        int duration,
        int maxStacks,
        EffectValue value)
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
        public override IEffect Copy() => new AdditionalHitChanceEffect(Duration, MaxStacks, Authored);
    }
}

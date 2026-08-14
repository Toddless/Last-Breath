namespace Battle.Source.Abilities.IncreasingPressure
{
    using Core.Battle.Abilities;
    using Core.Modifiers.Context;

    /// <summary>L2 upgrade: the first attack has +130% critical damage bonus.</summary>
    public class IpAugmentFirstAttackCritDamage(string id, string[] tags, int tier, FirstAttackCritContextModifier contextModifier)
        : AbilityAugment<IncreasingPressure>(id, tags, tier)
    {
        public override void ApplyUpgrade(IncreasingPressure ability) => ability.AddAttackModifier(contextModifier);

        public override void RemoveUpgrade(IncreasingPressure ability) => ability.RemoveAttackModifier(contextModifier.Id);

        public override IAbilityAugmentWrap<IncreasingPressure> Copy() =>
            new IpAugmentFirstAttackCritDamage(Id, Tags, Tier, contextModifier);
    }
}

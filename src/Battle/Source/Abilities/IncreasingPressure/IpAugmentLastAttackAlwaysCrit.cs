namespace Battle.Source.Abilities.IncreasingPressure
{
    using Core.Battle.Abilities;
    using Core.Modifiers.Context;

    /// <summary>L3 upgrade: the last attack in the series always scores a critical hit.</summary>
    public class IpAugmentLastAttackAlwaysCrit(string id, string[] tags, int tier, LastAttackAlwaysCritContextModifier contextModifier)
        : AbilityAugment<IncreasingPressure>(id, tags, tier)
    {
        public override void ApplyUpgrade(IncreasingPressure ability) => ability.AddAttackModifier(contextModifier);

        public override void RemoveUpgrade(IncreasingPressure ability) => ability.RemoveAttackModifier(contextModifier.Id);

        public override IAbilityAugmentWrap<IncreasingPressure> Copy() =>
            new IpAugmentLastAttackAlwaysCrit(Id, Tags, Tier, contextModifier);
    }
}

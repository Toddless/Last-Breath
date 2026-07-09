namespace Battle.Source.Abilities.IncreasingPressure
{
    using Core.Battle.Abilities;
    using Core.Modifiers.Context;

    /// <summary>L3 upgrade: the last attack in the series always scores a critical hit.</summary>
    public class IpUpgradeLastAttackAlwaysCrit(string id, string[] tags, int tier, LastAttackAlwaysCritContextModifier contextModifier)
        : AbilityUpgrade<IncreasingPressure>(id, tags, tier)
    {
        public override void ApplyUpgrade(IncreasingPressure ability) => ability.AddAttackModifier(contextModifier);

        public override void RemoveUpgrade(IncreasingPressure ability) => ability.RemoveAttackModifier(contextModifier.Id);

        public override IAbilityUpgradeWrap<IncreasingPressure> Copy() =>
            new IpUpgradeLastAttackAlwaysCrit(Id, Tags, Tier, contextModifier);
    }
}

namespace Battle.Source.Abilities.IncreasingPressure
{
    using Core.Interfaces.Abilities;

    /// <summary>L3 upgrade: the last attack in the series always scores a critical hit.</summary>
    public class IpUpgradeLastAttackAlwaysCrit(string id, string[] tags, int tier, LastAttackAlwaysCritModifier modifier)
        : AbilityUpgrade<IncreasingPressure>(id, tags, tier)
    {
        public override void ApplyUpgrade(IncreasingPressure ability) => ability.AddAttackModifier(modifier);

        public override void RemoveUpgrade(IncreasingPressure ability) => ability.RemoveAttackModifier(modifier.Id);

        public override IAbilityUpgradeWrap<IncreasingPressure> Copy() =>
            new IpUpgradeLastAttackAlwaysCrit(Id, Tags, Tier, modifier);
    }
}

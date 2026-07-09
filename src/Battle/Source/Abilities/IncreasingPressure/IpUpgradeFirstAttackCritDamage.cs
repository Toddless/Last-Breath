namespace Battle.Source.Abilities.IncreasingPressure
{
    using Core.Battle.Abilities;
    using Core.Modifiers.Context;

    /// <summary>L2 upgrade: the first attack has +130% critical damage bonus.</summary>
    public class IpUpgradeFirstAttackCritDamage(string id, string[] tags, int tier, FirstAttackCritContextModifier contextModifier)
        : AbilityUpgrade<IncreasingPressure>(id, tags, tier)
    {
        public override void ApplyUpgrade(IncreasingPressure ability) => ability.AddAttackModifier(contextModifier);

        public override void RemoveUpgrade(IncreasingPressure ability) => ability.RemoveAttackModifier(contextModifier.Id);

        public override IAbilityUpgradeWrap<IncreasingPressure> Copy() =>
            new IpUpgradeFirstAttackCritDamage(Id, Tags, Tier, contextModifier);
    }
}

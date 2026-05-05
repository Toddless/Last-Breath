namespace Battle.Source.Abilities.IncreasingPressure
{
    using Core.Interfaces.Abilities;

    /// <summary>L2 upgrade: the first attack has +130% critical damage bonus.</summary>
    public class IpUpgradeFirstAttackCritDamage(string id, string[] tags, int tier, FirstAttackCritModifier modifier)
        : AbilityUpgrade<IncreasingPressure>(id, tags, tier)
    {
        public override void ApplyUpgrade(IncreasingPressure ability) => ability.ExecutionStrategy.AddAttackModifier(modifier);

        public override void RemoveUpgrade(IncreasingPressure ability) => ability.ExecutionStrategy.RemoveAttackModifier(modifier);

        public override IAbilityUpgradeWrap<IncreasingPressure> Clone() =>
            new IpUpgradeFirstAttackCritDamage(Id, Tags, Tier, modifier);
    }
}

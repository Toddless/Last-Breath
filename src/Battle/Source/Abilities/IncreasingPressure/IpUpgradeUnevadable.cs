namespace Battle.Source.Abilities.IncreasingPressure
{
    using Core.Interfaces.Abilities;

    /// <summary>L2 upgrade: attacks cannot be evaded.</summary>
    public class IpUpgradeUnevadable(string id, string[] tags, int tier, UnevadableAttackModifier modifier)
        : AbilityUpgrade<IncreasingPressure>(id, tags, tier)
    {
        public override void ApplyUpgrade(IncreasingPressure ability) => ability.ExecutionStrategy.AddAttackModifier(modifier);

        public override void RemoveUpgrade(IncreasingPressure ability) => ability.ExecutionStrategy.RemoveAttackModifier(modifier);

        public override IAbilityUpgradeWrap<IncreasingPressure> Copy() =>
            new IpUpgradeUnevadable(Id, Tags, Tier, modifier);
    }
}

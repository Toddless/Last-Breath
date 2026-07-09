namespace Battle.Source.Abilities.IncreasingPressure
{
    using Core.Battle.Abilities;
    using Core.Modifiers.Context;

    /// <summary>L2 upgrade: attacks cannot be evaded.</summary>
    public class IpUpgradeUnevadable(string id, string[] tags, int tier, UnevadableAttackContextModifier contextModifier)
        : AbilityUpgrade<IncreasingPressure>(id, tags, tier)
    {
        public override void ApplyUpgrade(IncreasingPressure ability) => ability.AddAttackModifier(contextModifier);

        public override void RemoveUpgrade(IncreasingPressure ability) => ability.RemoveAttackModifier(contextModifier.Id);

        public override IAbilityUpgradeWrap<IncreasingPressure> Copy() =>
            new IpUpgradeUnevadable(Id, Tags, Tier, contextModifier);
    }
}

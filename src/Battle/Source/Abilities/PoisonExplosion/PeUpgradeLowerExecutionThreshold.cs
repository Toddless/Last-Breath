namespace Battle.Source.Abilities.PoisonExplosion
{
    using Core.Interfaces.Abilities;

    /// <summary>L2 upgrade: lowers the poison stack count required for execution by 5.</summary>
    public class PeUpgradeLowerExecutionThreshold(string id, string[] tags, int tier, int reduction = 5)
        : AbilityUpgrade<PoisonExplosion>(id, tags, tier)
    {
        public override void ApplyUpgrade(PoisonExplosion ability) =>
            ability.ExecutionThreshold -= reduction;

        public override void RemoveUpgrade(PoisonExplosion ability) =>
            ability.ExecutionThreshold += reduction;

        public override IAbilityUpgradeWrap<PoisonExplosion> Copy() =>
            new PeUpgradeLowerExecutionThreshold(Id, Tags, Tier, reduction);
    }
}

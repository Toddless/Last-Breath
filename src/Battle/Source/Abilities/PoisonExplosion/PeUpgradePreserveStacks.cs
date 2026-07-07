namespace Battle.Source.Abilities.PoisonExplosion
{
    using Core.Battle.Abilities;

    /// <summary>
    /// L3 upgrade: deals poison burst damage WITHOUT removing the stacks.
    /// </summary>
    public class PeUpgradePreserveStacks(string id, string[] tags, int tier)
        : AbilityUpgrade<PoisonExplosion>(id, tags, tier)
    {
        public override void ApplyUpgrade(PoisonExplosion ability) => ability.PreserveStacks = true;

        public override void RemoveUpgrade(PoisonExplosion ability) => ability.PreserveStacks = false;

        public override IAbilityUpgradeWrap<PoisonExplosion> Copy() =>
            new PeUpgradePreserveStacks(Id, Tags, Tier);
    }
}

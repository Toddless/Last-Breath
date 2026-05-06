namespace Battle.Source.Abilities.PoisonExplosion
{
    using Core.Interfaces.Abilities;

    /// <summary>
    /// L3 upgrade: instead of exploding, spreads all poison stacks from the target to all other enemies.
    /// </summary>
    public class PeUpgradeSpreadPoison(string id, string[] tags, int tier)
        : AbilityUpgrade<PoisonExplosion>(id, tags, tier)
    {
        public override void ApplyUpgrade(PoisonExplosion ability) => ability.SpreadMode = true;

        public override void RemoveUpgrade(PoisonExplosion ability) => ability.SpreadMode = false;

        public override IAbilityUpgradeWrap<PoisonExplosion> Clone() =>
            new PeUpgradeSpreadPoison(Id, Tags, Tier);
    }
}

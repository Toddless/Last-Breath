namespace Battle.Source.Abilities.PoisonExplosion
{
    using Core.Interfaces.Abilities;

    /// <summary>
    /// L3 upgrade: instead of exploding, spreads all poison stacks from the target to all other enemies.
    /// </summary>
    public class PeUpgradeSpreadPoison(string id, string[] tags, int tier)
        : AbilityUpgrade<PoisonExplosion>(id, tags, tier)
    {
        public override void ApplyUpgrade(PoisonExplosion ability) => ability.SpreadMode = new SpreadPoisonToAll();

        public override void RemoveUpgrade(PoisonExplosion ability) => ability.SpreadMode = null;

        public override IAbilityUpgradeWrap<PoisonExplosion> Copy() =>
            new PeUpgradeSpreadPoison(Id, Tags, Tier);
    }
}

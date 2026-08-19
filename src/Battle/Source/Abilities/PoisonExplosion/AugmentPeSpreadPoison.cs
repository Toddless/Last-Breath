namespace Battle.Source.Abilities.PoisonExplosion
{
    using Core.Battle.Abilities;

    /// <summary>
    /// L3 upgrade: instead of exploding, spreads all poison stacks from the target to all other enemies.
    /// </summary>
    public class AugmentPeSpreadPoison(string id, string[] tags, int tier)
        : Augment<PoisonExplosion>(id, tags, tier)
    {
        private IPoisonSpreadMode? _previousMode;

        public override void ApplyUpgrade(PoisonExplosion ability)
        {
            _previousMode = ability.SpreadMode;
            ability.SpreadMode = new SpreadPoisonToAll();
        }

        public override void RemoveUpgrade(PoisonExplosion ability) => ability.SpreadMode = _previousMode;

        public override IAugmentWrap<PoisonExplosion> Copy() =>
            new AugmentPeSpreadPoison(Id, Tags, Tier);
    }
}

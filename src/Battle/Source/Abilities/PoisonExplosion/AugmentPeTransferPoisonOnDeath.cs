namespace Battle.Source.Abilities.PoisonExplosion
{
    using Core.Battle.Abilities;

    /// <summary>
    /// L2 upgrade: when the target dies, its remaining poison stacks transfer to a random enemy.
    /// </summary>
    public class AugmentPeTransferPoisonOnDeath(string id, string[] tags, int tier)
        : Augment<PoisonExplosion>(id, tags, tier)
    {
        private IPoisonSpreadMode? _previousMode;

        public override void ApplyUpgrade(PoisonExplosion ability)
        {
            _previousMode = ability.SpreadMode;
            ability.SpreadMode = new SpreadPoisonToRandomTarget();
        }

        public override void RemoveUpgrade(PoisonExplosion ability) => ability.SpreadMode = _previousMode;

        public override IAugmentWrap<PoisonExplosion> Copy() =>
            new AugmentPeTransferPoisonOnDeath(Id, Tags, Tier);
    }
}

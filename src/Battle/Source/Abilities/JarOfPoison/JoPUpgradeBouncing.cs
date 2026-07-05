namespace Battle.Source.Abilities.JarOfPoison
{
    using Core.Interfaces.Abilities;
    using HitDelivery;

    /// <summary>L3 upgrade: the jar bounces 5 times between random enemies, applying poison each time.</summary>
    public class JoPUpgradeBouncing(string id, string[] tags, int tier, int bounces)
        : AbilityUpgrade<JarOfPoison>(id, tags, tier)
    {
        private IHitSequenceStrategy? _previousDelivery;

        public override void ApplyUpgrade(JarOfPoison ability)
        {
            _previousDelivery = ability.HitSequence;
            ability.HitSequence = new BouncingHits(bounces);
        }

        public override void RemoveUpgrade(JarOfPoison ability)
        {
            if (_previousDelivery == null) return;
            ability.HitSequence = _previousDelivery;
            _previousDelivery = null;
        }

        public override IAbilityUpgradeWrap<JarOfPoison> Copy() =>
            new JoPUpgradeBouncing(Id, Tags, Tier, bounces);
    }
}

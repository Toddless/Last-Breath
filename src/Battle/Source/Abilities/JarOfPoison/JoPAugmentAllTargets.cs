namespace Battle.Source.Abilities.JarOfPoison
{
    using Core.Battle.Abilities;
    using HitDelivery;

    /// <summary>L3 upgrade: applies poison to all enemies on the battlefield.</summary>
    public class JoPAugmentAllTargets(string id, string[] tags, int tier)
        : AbilityAugment<JarOfPoison>(id, tags, tier)
    {
        private IHitSequenceStrategy? _previousDelivery;

        public override void ApplyUpgrade(JarOfPoison ability)
        {
            _previousDelivery = ability.HitSequence;
            ability.HitSequence = new AllEnemiesHits();
        }

        public override void RemoveUpgrade(JarOfPoison ability)
        {
            if (_previousDelivery == null) return;
            ability.HitSequence = _previousDelivery;
            _previousDelivery = null;
        }

        public override IAbilityAugmentWrap<JarOfPoison> Copy() =>
            new JoPAugmentAllTargets(Id, Tags, Tier);
    }
}

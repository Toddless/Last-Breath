namespace Battle.Source.Abilities.IceShards
{
    using Core.Battle.Abilities;
    using Effects;

    /// <summary>L3 upgrade: every landed shard applies a Fragility stack to its target.</summary>
    public class IsUpgradeApplyFragility(string id, string[] tags, int tier, int duration, int maxStacks, float critDamageAmp)
        : AbilityUpgrade<IceShards>(id, tags, tier)
    {
        public override void ApplyUpgrade(IceShards ability) =>
            ability.ShardEffectFactory = () => new FragilityEffect(duration, maxStacks, critDamageAmp);

        public override void RemoveUpgrade(IceShards ability) => ability.ShardEffectFactory = null;

        public override IAbilityUpgradeWrap<IceShards> Copy() =>
            new IsUpgradeApplyFragility(Id, Tags, Tier, duration, maxStacks, critDamageAmp);
    }
}

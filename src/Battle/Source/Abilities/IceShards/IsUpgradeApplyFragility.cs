namespace Battle.Source.Abilities.IceShards
{
    using Effects;
    using Riders;

    /// <summary>L3 upgrade: every impact of the cast applies a Fragility stack — landed shards AND
    /// every victim of a stage-4 shrapnel burst (the rider fires per impact, whatever its source).</summary>
    public class IsUpgradeApplyFragility(string id, string[] tags, int tier, int duration, int maxStacks, float critDamageAmp)
        : AbilityUpgradeImpactRider(id, tags, tier, new ApplyEffectImpactRider(new FragilityEffect(duration, maxStacks, critDamageAmp)));
}

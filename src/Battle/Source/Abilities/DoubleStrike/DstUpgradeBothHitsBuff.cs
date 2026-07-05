namespace Battle.Source.Abilities.DoubleStrike
{
    using Core.Interfaces.Abilities;
    using Effects;

    /// <summary>L3 upgrade: landing BOTH strikes grants a damage buff for a few turns.</summary>
    public class DstUpgradeBothHitsBuff(string id, string[] tags, int tier, float amount, int duration)
        : AbilityUpgrade<DoubleStrike>(id, tags, tier)
    {
        public override void ApplyUpgrade(DoubleStrike ability) =>
            ability.BothHitsBuffFactory = () => new DamageBuffEffect(duration, maxStacks: 1, amount);

        public override void RemoveUpgrade(DoubleStrike ability) => ability.BothHitsBuffFactory = null;

        public override IAbilityUpgrade Copy() => new DstUpgradeBothHitsBuff(Id, Tags, Tier, amount, duration);
    }
}

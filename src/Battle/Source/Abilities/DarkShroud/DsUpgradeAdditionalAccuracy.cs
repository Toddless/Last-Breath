namespace Battle.Source.Abilities.DarkShroud
{
    using Core.Interfaces.Abilities;
    using Effects;

    /// <summary>
    /// L3 upgrade: while the shroud is active, the caster gains additional accuracy.
    /// Applied to the caster on cast via a post-activation modifier.
    /// </summary>
    public class DsUpgradeAdditionalAccuracy(string id, string[] tags, int tier, float amount)
        : AbilityUpgrade<DarkShroud>(id, tags, tier)
    {
        private string _modifierId = string.Empty;


        public override void ApplyUpgrade(DarkShroud ability)
        {
            var modifier = new AbilityBuffPostActivationModifier(new AccuracyBuff((int)ability.Duration, (int)ability.Stacks, amount));
            _modifierId = modifier.Id;
            ability.PostActivationEffect.TryAdd(modifier.Id, modifier);
        }

        public override void RemoveUpgrade(DarkShroud ability) => ability.PostActivationEffect.Remove(_modifierId);

        public override IAbilityUpgrade Copy() => new DsUpgradeAdditionalAccuracy(Id, Tags, Tier, amount);
    }
}

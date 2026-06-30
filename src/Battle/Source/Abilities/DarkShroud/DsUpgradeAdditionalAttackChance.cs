namespace Battle.Source.Abilities.DarkShroud
{
    using Core.Interfaces.Abilities;
    using Effects;

    /// <summary>
    /// L3 upgrade: while the shroud is active, the caster gains additional-attack chance.
    /// Applied to the caster on cast via a post-activation modifier.
    /// </summary>
    public class DsUpgradeAdditionalAttackChance(string id, string[] tags, int tier, float amount)
        : AbilityUpgrade<DarkShroud>(id, tags, tier)
    {
        private string _modifierId = string.Empty;

        public override void ApplyUpgrade(DarkShroud ability)
        {
            var modifier = new AbilityBuffPostActivationModifier(new AdditionalHitChanceEffect((int)ability.Duration, (int)ability.Stacks, amount));
            _modifierId = modifier.Id;
            ability.PostActivationEffect.TryAdd(modifier.Id, modifier);
        }

        public override void RemoveUpgrade(DarkShroud ability) => ability.PostActivationEffect.Remove(_modifierId);

        public override IAbilityUpgrade Copy() => new DsUpgradeAdditionalAttackChance(Id, Tags, Tier, amount);
    }
}

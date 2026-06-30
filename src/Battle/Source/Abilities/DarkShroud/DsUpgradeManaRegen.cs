namespace Battle.Source.Abilities.DarkShroud
{
    using Core.Interfaces.Abilities;
    using Effects;

    /// <summary>
    /// L2 upgrade: casting the shroud additionally restores a percentage of the caster's maximum mana.
    /// </summary>
    public class DsUpgradeManaRegen(string id, string[] tags, int tier, float regenAmount)
        : AbilityUpgrade<DarkShroud>(id, tags, tier)
    {
        private string _modifierId = string.Empty;

        public override void ApplyUpgrade(DarkShroud ability)
        {
            var modifier = new AbilityBuffPostActivationModifier(new ManaRegenerationEffect(regenAmount * ability.Effectiveness, (int)ability.Duration, (int)ability.Stacks));
            _modifierId = modifier.Id;
            ability.PostActivationEffect.TryAdd(modifier.Id, modifier);
        }

        public override void RemoveUpgrade(DarkShroud ability) => ability.PostActivationEffect.Remove(_modifierId);

        public override IAbilityUpgrade Copy() => new DsUpgradeManaRegen(Id, Tags, Tier, regenAmount);
    }
}

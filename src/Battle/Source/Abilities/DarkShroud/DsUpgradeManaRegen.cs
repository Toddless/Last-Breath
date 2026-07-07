namespace Battle.Source.Abilities.DarkShroud
{
    using Core.Battle.Abilities;
    using Effects;
    using Riders;

    /// <summary>
    /// L2 upgrade: casting the shroud additionally restores a percentage of the caster's maximum mana.
    /// </summary>
    public class DsUpgradeManaRegen(string id, string[] tags, int tier, float regenAmount)
        : AbilityUpgrade<DarkShroud>(id, tags, tier)
    {
        private string _modifierId = string.Empty;

        public override void ApplyUpgrade(DarkShroud ability)
        {
            var modifier = new DeferredEffectActivationRider(
                "Ability_Apply_Effect_Mana_Regeneration_Post_Activation_Modifier",
                () => new ManaRegenerationEffect(regenAmount * ability.Effectiveness, (int)ability.Duration, 1));
            _modifierId = modifier.Id;
            ability.ActivationRiders.TryAdd(modifier.Id, modifier);
        }

        public override void RemoveUpgrade(DarkShroud ability) => ability.ActivationRiders.Remove(_modifierId);

        public override IAbilityUpgrade Copy() => new DsUpgradeManaRegen(Id, Tags, Tier, regenAmount);
    }
}

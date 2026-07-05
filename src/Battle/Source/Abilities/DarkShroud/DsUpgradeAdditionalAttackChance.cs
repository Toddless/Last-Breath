namespace Battle.Source.Abilities.DarkShroud
{
    using Core.Interfaces.Abilities;
    using Effects;
    using Modifiers;

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
            var modifier = new DeferredEffectPostActivationModifier(
                "Ability_Apply_Effect_Additional_Hit_Chance_Buff_Post_Activation_Modifier",
                () => new AdditionalHitChanceEffect((int)ability.Duration, 1, amount));
            _modifierId = modifier.Id;
            ability.ActivationRiders.TryAdd(modifier.Id, modifier);
        }

        public override void RemoveUpgrade(DarkShroud ability) => ability.ActivationRiders.Remove(_modifierId);

        public override IAbilityUpgrade Copy() => new DsUpgradeAdditionalAttackChance(Id, Tags, Tier, amount);
    }
}

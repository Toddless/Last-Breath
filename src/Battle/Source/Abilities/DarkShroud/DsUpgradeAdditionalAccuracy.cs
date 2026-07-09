namespace Battle.Source.Abilities.DarkShroud
{
    using Core.Battle.Abilities;
    using Effects;
    using Riders;

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
            var modifier = new DeferredEffectActivationRider(
                "Ability_Apply_Effect_Accuracy_Buff_Post_Activation_Modifier",
                // Multiply wants the full multiplier: a raw 0.25 would CUT accuracy to a quarter.
                () => new AccuracyBuff((int)ability.Duration, (int)ability.Stacks, 1 + amount));
            _modifierId = modifier.Id;
            ability.ActivationRiders.TryAdd(modifier.Id, modifier);
        }

        public override void RemoveUpgrade(DarkShroud ability) => ability.ActivationRiders.Remove(_modifierId);

        public override IAbilityUpgrade Copy() => new DsUpgradeAdditionalAccuracy(Id, Tags, Tier, amount);
    }
}

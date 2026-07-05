namespace Battle.Source.Abilities.CriticalCalculation
{
    using Core.Interfaces.Abilities;
    using Effects;
    using Modifiers;

    /// <summary>
    /// L3: on cast, additionally raises the caster's critical chance for the ability's buff duration.
    /// </summary>
    public class CcUpgradeIncreaseCritChance(string id, string[] tags, int tier, float criticalChance)
        : AbilityUpgrade<CriticalCalculation>(id, tags, tier)
    {
        private string _modifierId = string.Empty;

        public override void ApplyUpgrade(CriticalCalculation ability)
        {
            var modifier = new DeferredEffectPostActivationModifier(
                "Ability_Apply_Cc_Crit_Chance_Buff_Post_Activation_Modifier",
                () => new CriticalChanceBuffEffect(ability.BuffDuration, 1, criticalChance));
            _modifierId = modifier.Id;
            ability.ActivationRiders.TryAdd(modifier.Id, modifier);
        }

        public override void RemoveUpgrade(CriticalCalculation ability) => ability.ActivationRiders.Remove(_modifierId);

        public override IAbilityUpgrade Copy() => new CcUpgradeIncreaseCritChance(Id, Tags, Tier, criticalChance);
    }
}

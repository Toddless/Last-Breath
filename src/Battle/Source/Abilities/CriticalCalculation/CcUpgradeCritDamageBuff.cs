namespace Battle.Source.Abilities.CriticalCalculation
{
    using Core.Interfaces.Abilities;
    using Effects;

    /// <summary>
    /// L2: on cast, buffs the caster with a critical-damage boost that also raises crit chance
    /// after each successful attack, for the given duration.
    /// </summary>
    public class CcUpgradeCritDamageBuff(string id, string[] tags, int tier, float critDamageBonus, float critDamagePerCritAttack, int duration)
        : AbilityUpgrade<CriticalCalculation>(id, tags, tier)
    {
        private readonly IAbilityPostActivationModifier _modifier =
            new AbilityBuffPostActivationModifier(new CritDamageOnHitBuff(critDamageBonus, duration, critDamagePerCritAttack));

        public override void ApplyUpgrade(CriticalCalculation ability) => ability.PostActivationEffect.TryAdd(_modifier.Id, _modifier);

        public override void RemoveUpgrade(CriticalCalculation ability) => ability.PostActivationEffect.Remove(_modifier.Id);

        public override IAbilityUpgrade Copy() => new CcUpgradeCritDamageBuff(Id, Tags, Tier, critDamageBonus, critDamagePerCritAttack, duration);
    }
}

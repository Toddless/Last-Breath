namespace Battle.Source.Abilities.CriticalCalculation
{
    using System;
    using Core.Battle.Abilities;
    using Effects;

    /// <summary>
    /// L3: replaces the ability's primary crit-chance buff with a self-extending additional-attack-chance
    /// buff (same stack/duration/extend-on-crit mechanic). Swaps the ability's primary buff factory.
    /// </summary>
    public class CcUpgradeAdditionalAttackChance(string id, string[] tags, int tier, float value = 0.15f)
        : AbilityUpgrade<CriticalCalculation>(id, tags, tier)
    {
        private Func<int, int, float, IEffect>? _previous;

        public override void ApplyUpgrade(CriticalCalculation ability)
        {
            _previous = ability.PrimaryBuffFactory;
            ability.PrimaryBuffFactory = (duration, maxStacks, effectiveness) =>
                new AttackChanceCalculationBuff(duration, maxStacks, value: value * effectiveness);
        }

        public override void RemoveUpgrade(CriticalCalculation ability)
        {
            if (_previous != null) ability.PrimaryBuffFactory = _previous;
        }

        public override IAbilityUpgrade Copy() => new CcUpgradeAdditionalAttackChance(Id, Tags, Tier, value);
    }
}

namespace Battle.Source.Abilities.CriticalCalculation
{
    using Effects;
    using Core.Interfaces.Abilities;

    /// <summary>
    /// L3 upgrade option: while the crit buff is active, also grants critical damage boost for 3 turns,
    /// and each successful attack increases the bearer's crit chance by 15%.
    /// </summary>
    public class CcUpgradeCritDamageBuff(string id, string[] tags, int tier,
        float critDamageBonus = 1.0f, float critChancePerHit = 0.15f, int critDamageDuration = 3)
        : AbilityUpgrade<CriticalCalculation>(id, tags, tier)
    {
        private IEffect? _critDmgEffect;

        public override void ApplyUpgrade(CriticalCalculation ability)
        {
            _critDmgEffect = new CritDamageOnHitBuff(
                critDamageBonus, critDamageDuration, critChancePerHit);
        }

        public override void RemoveUpgrade(CriticalCalculation ability)
        {
            _critDmgEffect = null;
        }

        public override IAbilityUpgradeWrap<CriticalCalculation> Copy() =>
            new CcUpgradeCritDamageBuff(Id, Tags, Tier, critDamageBonus, critChancePerHit, critDamageDuration);
    }
}

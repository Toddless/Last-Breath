namespace Battle.Source.Abilities.CriticalCalculation
{
    using Core.Interfaces.Abilities;
    using Core.Modifiers;
    /// <summary>
    /// L3 upgrade: replaces the crit chance buff with an additional hit chance buff
    /// (AdditionalHitChance), keeping the same stack/duration mechanics.
    /// </summary>
    public class CcUpgradeReplaceWithHitChanceBuff(string id, string[] tags, int tier)
        : AbilityUpgrade<CriticalCalculation>(id, tags, tier)
    {

        public override void ApplyUpgrade(CriticalCalculation ability)
        {
        }

        public override void RemoveUpgrade(CriticalCalculation ability)
        {
        }

        public override IAbilityUpgradeWrap<CriticalCalculation> Clone() =>
            new CcUpgradeReplaceWithHitChanceBuff(Id, Tags, Tier);
    }
}

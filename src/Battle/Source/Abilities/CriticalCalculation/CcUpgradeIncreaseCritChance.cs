namespace Battle.Source.Abilities.CriticalCalculation
{
    using Effects;
    using Core.Enums;
    using Core.Interfaces.Abilities;

    /// <summary>
    /// L2 upgrade: additionally increases the bearer's critical chance by a flat amount
    /// by adding an extra ParameterBuffEffect to the ability's effects list.
    /// </summary>
    public class CcUpgradeIncreaseCritChance(string id, string[] tags, int tier, float critChanceBonus = 0.05f)
        : AbilityUpgrade<CriticalCalculation>(id, tags, tier)
    {
        private IEffect? _addedEffect;

        public override void ApplyUpgrade(CriticalCalculation ability)
        {
        }

        public override void RemoveUpgrade(CriticalCalculation ability)
        {
        }

        public override IAbilityUpgradeWrap<CriticalCalculation> Copy() =>
            new CcUpgradeIncreaseCritChance(Id, Tags, Tier, critChanceBonus);
    }
}

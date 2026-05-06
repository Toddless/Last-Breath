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
            _addedEffect = new ParameterBuffEffect(
                id: "Effect_CC_Extra_Crit_Chance",
                duration: ability.BuffDuration,
                maxStacks: 999,
                amount: critChanceBonus,
                parameter: EntityParameter.CriticalChance,
                type: ModifierValueType.Flat);
        }

        public override void RemoveUpgrade(CriticalCalculation ability)
        {
        }

        public override IAbilityUpgradeWrap<CriticalCalculation> Clone() =>
            new CcUpgradeIncreaseCritChance(Id, Tags, Tier, critChanceBonus);
    }
}

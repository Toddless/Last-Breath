namespace Battle.Source.Abilities.ChainLightning
{
    using Core.Battle.Abilities;
    using Core.Components.Decorator;
    using Core.Enums;

    /// <summary>L1 upgrade: bigger damage scales at the price of an increased resource cost.</summary>
    public class ClUpgradeScalesAddCost(string id, string[] tags, int tier, float weaponScale, float spellScale, float additionalCost)
        : AbilityUpgrade<ChainLightning>(id, tags, tier)
    {
        private const string WeaponDecoratorId = "Ability_Parameter_Decorator_Cl_Weapon_Scale";
        private const string SpellDecoratorId = "Ability_Parameter_Decorator_Cl_Spell_Scale";
        private const string CostDecoratorId = "Ability_Parameter_Decorator_Cl_Scales_Cost";

        public override void ApplyUpgrade(ChainLightning ability)
        {
            ability.AddParameterDecorator(new SimpleAbilityParameterDecorator<AbilityParameter>(
                AbilityParameter.WeaponDamageScale, Priority.Weak, OperationType.Add, weaponScale, WeaponDecoratorId, Id));
            ability.AddParameterDecorator(new SimpleAbilityParameterDecorator<AbilityParameter>(
                AbilityParameter.SpellDamageScale, Priority.Weak, OperationType.Add, spellScale, SpellDecoratorId, Id));
            ability.AddParameterDecorator(new SimpleAbilityParameterDecorator<AbilityParameter>(
                AbilityParameter.CostValue, Priority.Weak, OperationType.Add, additionalCost, CostDecoratorId, Id));
        }

        public override void RemoveUpgrade(ChainLightning ability)
        {
            ability.RemoveParameterDecorator(WeaponDecoratorId, AbilityParameter.WeaponDamageScale);
            ability.RemoveParameterDecorator(SpellDecoratorId, AbilityParameter.SpellDamageScale);
            ability.RemoveParameterDecorator(CostDecoratorId, AbilityParameter.CostValue);
        }

        public override IAbilityUpgrade Copy() => new ClUpgradeScalesAddCost(Id, Tags, Tier, weaponScale, spellScale, additionalCost);
    }
}

namespace Battle.Source.Abilities
{
    using Core.Battle.Abilities;
    using Core.Enums;
    using Decorators;

    /// <summary>Adds flat bonuses to the ability's weapon and spell damage scales.</summary>
    public class AbilityUpgradeAdditionalScales(string id, string[] tags, int tier, float weaponScale, float spellScale)
        : AbilityUpgrade<DamagingAbility>(id, tags, tier)
    {
        private const string WeaponDecoratorId = "Ability_Parameter_Decorator_Additional_Weapon_Scale";
        private const string SpellDecoratorId = "Ability_Parameter_Decorator_Additional_Spell_Scale";

        public override void ApplyUpgrade(DamagingAbility ability)
        {
            ability.AddParameterDecorator(new SimpleAbilityParameterDecorator<AbilityParameter>(
                AbilityParameter.WeaponDamageScale, Priority.Weak, OperationType.Add, weaponScale, WeaponDecoratorId, Id));
            ability.AddParameterDecorator(new SimpleAbilityParameterDecorator<AbilityParameter>(
                AbilityParameter.SpellDamageScale, Priority.Weak, OperationType.Add, spellScale, SpellDecoratorId, Id));
        }

        public override void RemoveUpgrade(DamagingAbility ability)
        {
            ability.RemoveParameterDecorator(WeaponDecoratorId, AbilityParameter.WeaponDamageScale);
            ability.RemoveParameterDecorator(SpellDecoratorId, AbilityParameter.SpellDamageScale);
        }

        public override IAbilityUpgrade Copy() => new AbilityUpgradeAdditionalScales(Id, Tags, Tier, weaponScale, spellScale);
    }
}

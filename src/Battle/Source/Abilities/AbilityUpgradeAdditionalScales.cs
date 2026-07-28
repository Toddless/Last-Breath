namespace Battle.Source.Abilities
{
    using Core.Battle.Abilities;
    using Core.Enums;

    /// <summary>Adds flat bonuses to the ability's weapon and spell damage scales. Typed to the base
    /// Ability: the keys are registered by every damage-dealing ability (multicast ones included);
    /// applying to an ability without them is a loud Tracker warning.</summary>
    public class AbilityUpgradeAdditionalScales(string id, string[] tags, int tier, float weaponScale, float spellScale)
        : AbilityUpgrade<Ability>(id, tags, tier)
    {
        private string WeaponDecoratorId => $"Ability_Parameter_Decorator_{Id}_Weapon_Scale";
        private string SpellDecoratorId => $"Ability_Parameter_Decorator_{Id}_Spell_Scale";

        public override void ApplyUpgrade(Ability ability)
        {
            ability.AddParameterDecorator(new SimpleAbilityParameterDecorator(
                AbilityParameter.WeaponDamageScale, Priority.Weak, OperationType.Add, weaponScale, WeaponDecoratorId, Id));
            ability.AddParameterDecorator(new SimpleAbilityParameterDecorator(
                AbilityParameter.SpellDamageScale, Priority.Weak, OperationType.Add, spellScale, SpellDecoratorId, Id));
        }

        public override void RemoveUpgrade(Ability ability)
        {
            ability.RemoveParameterDecorator(WeaponDecoratorId, AbilityParameter.WeaponDamageScale);
            ability.RemoveParameterDecorator(SpellDecoratorId, AbilityParameter.SpellDamageScale);
        }

        public override IAbilityUpgrade Copy() => new AbilityUpgradeAdditionalScales(Id, Tags, Tier, weaponScale, spellScale);
    }
}

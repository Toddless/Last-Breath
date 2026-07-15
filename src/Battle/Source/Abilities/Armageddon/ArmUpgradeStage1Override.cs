namespace Battle.Source.Abilities.Armageddon
{
    using Core.Battle.Abilities;
    using Core.Entity.Components.Decorator;
    using Core.Enums;

    /// <summary>L2 upgrade: replaces the stage-1 numbers entirely (e.g. 300+(75%+75%) → 400+(100%+100%)).</summary>
    public class ArmUpgradeStage1Override(string id, string[] tags, int tier, float damage, float weaponScale, float spellScale)
        : AbilityUpgrade<Armageddon>(id, tags, tier)
    {
        private const string DamageDecoratorId = "Ability_Parameter_Decorator_Arm_Stage1_Damage";
        private const string WeaponDecoratorId = "Ability_Parameter_Decorator_Arm_Stage1_Weapon";
        private const string SpellDecoratorId = "Ability_Parameter_Decorator_Arm_Stage1_Spell";

        public override void ApplyUpgrade(Armageddon ability)
        {
            ability.AddParameterDecorator(new SimpleAbilityParameterDecorator<AbilityParameter>(
                AbilityParameter.Damage, Priority.Weak, OperationType.Override, damage, DamageDecoratorId, Id));
            ability.AddParameterDecorator(new SimpleAbilityParameterDecorator<AbilityParameter>(
                AbilityParameter.WeaponDamageScale, Priority.Weak, OperationType.Override, weaponScale, WeaponDecoratorId, Id));
            ability.AddParameterDecorator(new SimpleAbilityParameterDecorator<AbilityParameter>(
                AbilityParameter.SpellDamageScale, Priority.Weak, OperationType.Override, spellScale, SpellDecoratorId, Id));
        }

        public override void RemoveUpgrade(Armageddon ability)
        {
            ability.RemoveParameterDecorator(DamageDecoratorId, AbilityParameter.Damage);
            ability.RemoveParameterDecorator(WeaponDecoratorId, AbilityParameter.WeaponDamageScale);
            ability.RemoveParameterDecorator(SpellDecoratorId, AbilityParameter.SpellDamageScale);
        }

        public override IAbilityUpgrade Copy() => new ArmUpgradeStage1Override(Id, Tags, Tier, damage, weaponScale, spellScale);
    }
}

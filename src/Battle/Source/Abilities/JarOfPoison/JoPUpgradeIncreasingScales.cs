namespace Battle.Source.Abilities.JarOfPoison
{
    using Core.Battle.Abilities;
    using Core.Enums;
    using Decorators;

    public class JoPUpgradeIncreasingScales(string id, string[] tags, int tier, float weaponScale, float spellScale)
        : AbilityUpgrade<JarOfPoison>(id, tags, tier)
    {
        private const string WeaponScaleDecoratorId = "Ability_Parameter_Decorator_JoP_Weapon_Scale";
        private const string SpellScaleDecoratorId = "Ability_Parameter_Decorator_JoP_Spell_Scale";

        public override void ApplyUpgrade(JarOfPoison ability)
        {
            var weaponScaleDecorator = new SimpleAbilityParameterDecorator<AbilityParameter>(AbilityParameter.WeaponDamageScale, Priority.Weak, OperationType.Add, weaponScale,
                WeaponScaleDecoratorId, Id);
            var spellScaleDecorator = new SimpleAbilityParameterDecorator<AbilityParameter>(AbilityParameter.SpellDamageScale, Priority.Weak, OperationType.Add, spellScale,
                SpellScaleDecoratorId, Id);
            ability.AddParameterDecorator(weaponScaleDecorator);
            ability.AddParameterDecorator(spellScaleDecorator);
        }

        public override void RemoveUpgrade(JarOfPoison ability)
        {
            ability.RemoveParameterDecorator(WeaponScaleDecoratorId, AbilityParameter.WeaponDamageScale);
            ability.RemoveParameterDecorator(SpellScaleDecoratorId, AbilityParameter.SpellDamageScale);
        }

        public override IAbilityUpgrade Copy() => new JoPUpgradeIncreasingScales(Id, Tags, Tier, weaponScale, spellScale);
    }
}

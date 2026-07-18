namespace Battle.Source.PassiveSkills
{
    using Core.Battle.Skills;
    using Core.Entity;
    using Core.Enums;
    using Core.Modifiers;

    /// <summary>"Armor Piercing": the owner's physical damage ignores the target's armor
    /// (armor penetration is driven to 100%).</summary>
    public class ArmorPiercingPassiveSkill() : Skill(id: "Passive_Skill_Armor_Piercing")
    {
        private IModifierInstance? _modifier;

        public override void Attach(IFightable owner)
        {
            Owner = owner;
            _modifier = new SimpleModifier(EntityParameter.ArmorPenetration, ModifierValueType.Flat, 1f, InstanceId);
            owner.ParameterModifiers.AddModifier(_modifier);
        }

        public override void Detach(IFightable owner)
        {
            if (_modifier != null) owner.ParameterModifiers.RemoveModifier(_modifier);
            _modifier = null;
            Owner = null;
        }

        public override ISkill Copy() => new ArmorPiercingPassiveSkill();

        public override bool IsStronger(ISkill skill) => false;
    }
}

namespace Battle.Source.PassiveSkills
{
    using Core.Battle.Skills;
    using Core.Context;
    using Core.Entity;
    using Core.Modifiers.Context;

    /// <summary>"Бездушный": the owner's attacks and ability hits bypass the target's barrier.</summary>
    public class SoullessPassiveSkill() : Skill(id: "Passive_Skill_Soulless")
    {
        private IDamageModifier? _modifier;

        public override void Attach(IFightable owner)
        {
            Owner = owner;
            _modifier = new BarrierBypassContextModifier(owner);
            owner.ModifierHandler.Add(_modifier);
        }

        public override void Detach(IFightable owner)
        {
            if (_modifier != null) owner.ModifierHandler.Remove(_modifier);
            _modifier = null;
            Owner = null;
        }

        public override ISkill Copy() => new SoullessPassiveSkill();

        public override bool IsStronger(ISkill skill) => false;
    }
}

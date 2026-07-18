namespace Battle.Source.PassiveSkills
{
    using Core.Battle.Skills;
    using Core.Entity;
    using Core.Events;

    /// <summary>"Точно в цель": the owner's attacks cannot be evaded. BeforeAttack fires only for
    /// the owner's own attacks (the event lives on the attacker's bus), so no source gate is needed.</summary>
    public class TrueStrikePassiveSkill() : Skill(id: "Passive_Skill_True_Strike")
    {
        public override void Attach(IFightable owner)
        {
            Owner = owner;
            owner.CombatEvents.Subscribe<BeforeAttackEvent>(OnBeforeAttack);
        }

        private void OnBeforeAttack(BeforeAttackEvent evt) => evt.Context.IsUnevadable = true;

        public override void Detach(IFightable owner)
        {
            owner.CombatEvents.Unsubscribe<BeforeAttackEvent>(OnBeforeAttack);
            Owner = null;
        }

        public override ISkill Copy() => new TrueStrikePassiveSkill();

        public override bool IsStronger(ISkill skill) => false;
    }
}

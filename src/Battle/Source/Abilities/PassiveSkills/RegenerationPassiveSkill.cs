namespace Battle.Source.Abilities.PassiveSkills
{
    using CombatEvents;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Skills;

    public class RegenerationPassiveSkill(string id, float regenAmount)
        : Skill(id)
    {
        private float RegenAmount { get; } = regenAmount;

        public override void Attach(IEntity owner)
        {
            owner.CombatEvents.Subscribe<TurnEndEvent>(OnTurnEnd);
        }

        private void OnTurnEnd(TurnEndEvent evnt)
        {
            evnt.Source.Heal(RegenAmount);
        }

        public override void Detach(IEntity owner)
        {
            owner.CombatEvents.Unsubscribe<TurnEndEvent>(OnTurnEnd);
        }

        public override ISkill Copy() => new RegenerationPassiveSkill(Id, RegenAmount);

        public override bool IsStronger(ISkill skill)
        {
            if (skill is not RegenerationPassiveSkill regenerationPassiveSkill) return false;

            return RegenAmount > regenerationPassiveSkill.RegenAmount;
        }
    }
}

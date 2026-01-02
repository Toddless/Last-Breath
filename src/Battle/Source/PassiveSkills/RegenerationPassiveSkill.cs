namespace Battle.Source.PassiveSkills
{
    using Core.Interfaces.Entity;
    using Core.Interfaces.Events.GameEvents;
    using Core.Interfaces.Skills;

    public class RegenerationPassiveSkill(float regenAmount)
        : Skill(id: "Passive_Skill_Regeneration")
    {
        private float RegenAmount { get; } = regenAmount;

        public override void Attach(IEntity owner)
        {
            Owner = owner;
            Owner.CombatEvents.Subscribe<TurnEndEvent>(OnTurnEnd);
        }

        private void OnTurnEnd(TurnEndEvent evnt)
        {
            float healAmount = Owner.Parameters.MaxHealth * RegenAmount;
            Owner?.Heal(healAmount);
        }

        public override void Detach(IEntity owner)
        {
            Owner?.CombatEvents.Unsubscribe<TurnEndEvent>(OnTurnEnd);
            Owner = null;
        }

        public override ISkill Copy() => new RegenerationPassiveSkill(RegenAmount);

        public override bool IsStronger(ISkill skill)
        {
            if (skill is not RegenerationPassiveSkill regenerationPassiveSkill) return false;

            return RegenAmount > regenerationPassiveSkill.RegenAmount;
        }
    }
}

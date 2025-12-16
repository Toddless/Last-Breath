namespace Battle.Source.Abilities.PassiveSkills
{
    using Core.Interfaces.Entity;
    using Core.Interfaces.Events.GameEvents;
    using Core.Interfaces.Skills;

    public class RegenerationPassiveSkill(string id, float regenAmount)
        : Skill(id)
    {
        private float RegenAmount { get; } = regenAmount;

        public override void Attach(IEntity owner)
        {
            Owner = owner;
            Owner.CombatEvents.Subscribe<TurnEndGameEvent>(OnTurnEnd);
        }

        private void OnTurnEnd(TurnEndGameEvent evnt)
        {
            Owner?.Heal(RegenAmount);
        }

        public override void Detach(IEntity owner)
        {
            Owner?.CombatEvents.Unsubscribe<TurnEndGameEvent>(OnTurnEnd);
            Owner = null;
        }

        public override ISkill Copy() => new RegenerationPassiveSkill(Id, RegenAmount);

        public override bool IsStronger(ISkill skill)
        {
            if (skill is not RegenerationPassiveSkill regenerationPassiveSkill) return false;

            return RegenAmount > regenerationPassiveSkill.RegenAmount;
        }
    }
}

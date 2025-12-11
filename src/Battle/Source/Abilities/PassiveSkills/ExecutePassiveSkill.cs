namespace Battle.Source.Abilities.PassiveSkills
{
    using CombatEvents;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Skills;

    public class ExecutePassiveSkill(string id, float percentToKill)
        : Skill(id)
    {
        public float PercentToKill { get; } = percentToKill;

        public override void Attach(IEntity owner)
        {
            owner.Events.Subscribe<AfterAttackEvent>(OnAfterAttack);
        }

        private void OnAfterAttack(AfterAttackEvent evnt)
        {
            var context = evnt.Context;

            float healthLeftInPercent = context.Target.CurrentHealth / context.Target.Parameters.MaxHealth;
            if (healthLeftInPercent <= PercentToKill) context.Target.Kill();
        }

        public override void Detach(IEntity owner)
        {
            owner.Events.Unsubscribe<AfterAttackEvent>(OnAfterAttack);
        }

        public override ISkill Copy() => new ExecutePassiveSkill(Id, PercentToKill);

        public override bool IsStronger(ISkill skill)
        {
            if (skill is not ExecutePassiveSkill execute) return false;
            return execute.PercentToKill > PercentToKill;
        }
    }
}

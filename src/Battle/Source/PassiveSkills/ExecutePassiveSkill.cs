namespace Battle.Source.PassiveSkills
{
    using Core.Battle.Skills;
    using Core.Entity;
    using Core.Events;

    public class ExecutePassiveSkill(float threshold)
        : Skill(id: "Passive_Skill_Execute")
    {
        public float Threshold { get; } = threshold;

        public override void Attach(IFightable owner)
        {
            owner.CombatEvents.Subscribe<AfterAttackEvent>(OnAfterAttack);
        }

        private void OnAfterAttack(AfterAttackEvent evnt)
        {
            var context = evnt.Context;

            float healthLeftInPercent = context.Target.CurrentHealth / context.Target.Parameters.MaxHealth;
            if (healthLeftInPercent <= Threshold) context.Target.Kill();
        }

        public override void Detach(IFightable owner)
        {
            owner.CombatEvents.Unsubscribe<AfterAttackEvent>(OnAfterAttack);
        }

        public override ISkill Copy() => new ExecutePassiveSkill(Threshold);

        public override bool IsStronger(ISkill skill)
        {
            if (skill is not ExecutePassiveSkill execute) return false;
            return execute.Threshold > Threshold;
        }
    }
}

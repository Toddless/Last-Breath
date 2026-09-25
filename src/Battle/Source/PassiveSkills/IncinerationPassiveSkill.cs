namespace Battle.Source.PassiveSkills
{
    using Core.Battle.Skills;
    using Core.Entity;
    using Core.Enums;
    using Core.Events;

    /// <summary>"Испепеление": a landed critical attack on a BURNING target kills it outright.</summary>
    public class IncinerationPassiveSkill() : Skill(id: "Passive_Skill_Incineration")
    {
        public override void Attach(IFightable owner)
        {
            Owner = owner;
            owner.CombatEvents.Subscribe<AfterAttackEvent>(OnAfterAttack);
        }

        private void OnAfterAttack(AfterAttackEvent evt)
        {
            var context = evt.Context;
            if (context.Result is not AttackResults.Succeed) return;
            if (!context.IsCritical && !context.ForceCriticalAttack) return;
            if ((context.Target.StatusEffects & StatusEffects.Burning) == 0) return;
            if (!context.Target.IsAlive) return;

            context.Target.Kill();
        }

        public override void Detach(IFightable owner)
        {
            owner.CombatEvents.Unsubscribe<AfterAttackEvent>(OnAfterAttack);
            Owner = null;
        }

        public override ISkill Copy() => new IncinerationPassiveSkill();

        /// <summary>Parameterless kill switch — every instance behaves identically,
        /// so no instance can be stronger than another.</summary>
        public override bool IsStronger(ISkill skill) => false;
    }
}

namespace Battle.Source.PassiveSkills
{
    using Core.Interfaces.Entity;
    using Core.Interfaces.Events.GameEvents;
    using Core.Interfaces.Skills;

    public class VampireAttackPassiveSkill(float leachPercent)
        : Skill(id: "Passive_Skill_Vampier")
    {
        public float LeachPercent { get; } = leachPercent;

        public override void Attach(IEntity owner)
        {
            Owner = owner;
            Owner.CombatEvents.Subscribe<AfterAttackEvent>(OnAfterAttack);
        }

        private void OnAfterAttack(AfterAttackEvent evnt)
        {
            float toHeal = evnt.Context.FinalDamage * LeachPercent;
            Owner?.Heal(toHeal);
        }

        public override void Detach(IEntity owner)
        {
            Owner?.CombatEvents.Unsubscribe<AfterAttackEvent>(OnAfterAttack);
            Owner = null;
        }

        public override ISkill Copy() => new VampireAttackPassiveSkill(LeachPercent);

        public override bool IsStronger(ISkill skill)
        {
            if (skill is not VampireAttackPassiveSkill vampire) return false;

            return vampire.LeachPercent > LeachPercent;
        }
    }
}

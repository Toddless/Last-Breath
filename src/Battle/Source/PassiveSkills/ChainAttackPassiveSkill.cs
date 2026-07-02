namespace Battle.Source.PassiveSkills
{
    using Core.Interfaces.Entity;
    using Core.Interfaces.Events.GameEvents;
    using Core.Interfaces.Skills;

    public class ChainAttackPassiveSkill()
        : Skill(id: "Passive_Skill_Chain_Attack")
    {
        public override void Attach(IFightable owner)
        {
            Owner = owner;
            owner.CombatEvents.Subscribe<AfterAttackEvent>(OnAfterAttack);
        }

        private void OnAfterAttack(AfterAttackEvent @event)
        {
            if (Owner == null) return;
            if (@event.Context.Rnd.Randf() > Owner.Parameters.AdditionalHit) return;
            var context = new AttackContext(Owner, @event.Context.Target,
                Owner.Parameters.Damage * @event.Context.Rnd.RandfRange(0.9f, 1.1f), @event.Context.Rnd, @event.Context.AttackContextScheduler)
            {
                RawCriticalChance = Owner.Parameters.CriticalChance
            };
            context.Schedule();
        }

        public override void Detach(IFightable owner) => owner.CombatEvents.Unsubscribe<AfterAttackEvent>(OnAfterAttack);

        public override ISkill Copy() => new ChainAttackPassiveSkill();

        public override bool IsStronger(ISkill skill) => false;
    }
}

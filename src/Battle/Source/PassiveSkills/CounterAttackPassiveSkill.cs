namespace Battle.Source.PassiveSkills
{
    using System;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Skills;
    using Core.Interfaces.Events.GameEvents;

    public class CounterAttackPassiveSkill(float chance = 0.5f) : Skill(id: "Passive_Skill_Counter_Attack")
    {
        public float Chance { get; } = chance;

        public override void Attach(IEntity owner)
        {
            Owner = owner;
            Owner.CombatEvents.Subscribe<AttackEvadedEvent>(OnAttackEvaded);
        }

        private void OnAttackEvaded(AttackEvadedEvent evnt)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(Owner);
                if (evnt.Context.Target.InstanceId != Owner.InstanceId) return;
                if (evnt.Context.Rnd.RandFloat() <= Chance) return;
                var context = new AttackContext(Owner, evnt.Context.Attacker, Owner.GetDamage(), new RndGodot(), evnt.Context.AttackContextScheduler);
                context.Schedule();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public override void Detach(IEntity owner)
        {
            owner.CombatEvents.Unsubscribe<AttackEvadedEvent>(OnAttackEvaded);
            Owner = null;
        }

        public override ISkill Copy() => new CounterAttackPassiveSkill(Chance);

        public override bool IsStronger(ISkill skill)
        {
            if (skill is not CounterAttackPassiveSkill counter) return false;
            return counter.Chance > Chance;
        }
    }
}

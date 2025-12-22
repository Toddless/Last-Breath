namespace Battle.Source.PassiveSkills
{
    using System;
    using TestData;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Events.GameEvents;
    using Core.Interfaces.Skills;

    public class CounterAttackPassiveSkill(float chance = 0.5f) : Skill(id: "Passive_Skill_Counter_Counter")
    {
        public float Chance { get; } = chance;

        public override void Attach(IEntity owner)
        {
            Owner = owner;
            Owner.CombatEvents.Subscribe<BeforeDamageTakenEvent>(OnDamageTaken);
        }

        private void OnDamageTaken(BeforeDamageTakenEvent evnt)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(Owner);
                if (evnt.Context.Rnd.RandFloat() > Chance) return;
                var context = new AttackContext(Owner, evnt.Context.Attacker, Owner.Parameters.Damage * evnt.Context.Rnd.RandFloatRange(0.9f, 1.1f), new RndGodot(),
                    evnt.Context.AttackContextScheduler);
                context.Schedule();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public override void Detach(IEntity owner)
        {
            owner.CombatEvents.Unsubscribe<BeforeDamageTakenEvent>(OnDamageTaken);
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

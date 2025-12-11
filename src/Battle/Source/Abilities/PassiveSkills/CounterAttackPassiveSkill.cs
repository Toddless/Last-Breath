namespace Battle.Source.Abilities.PassiveSkills
{
    using System;
    using TestData;
    using CombatEvents;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Skills;

    public class CounterAttackPassiveSkill(string id, float chance = 0.5f) : Skill(id)
    {
        public float Chance { get; } = chance;

        public override void Attach(IEntity owner)
        {
            owner.Events.Subscribe<DamageTakenEvent>(OnDamageTaken);
        }

        private void OnDamageTaken(DamageTakenEvent evnt)
        {
            try
            {
                if (evnt.Context.Rnd.RandFloat() > Chance) return;
                var context = new AttackContext(evnt.Source, evnt.Context.Attacker, evnt.Source.Parameters.Damage * evnt.Context.Rnd.RandFloatRange(0.9f, 1.1f), new RndGodot(),
                    evnt.Context.CombatScheduler);
                context.Schedule();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public override void Detach(IEntity owner) => throw new NotImplementedException();

        public override ISkill Copy() => throw new NotImplementedException();

        public override bool IsStronger(ISkill skill) => throw new NotImplementedException();
    }
}

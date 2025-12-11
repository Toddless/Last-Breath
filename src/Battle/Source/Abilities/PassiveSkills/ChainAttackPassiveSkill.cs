namespace Battle.Source.Abilities.PassiveSkills
{
    using CombatEvents;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Skills;
    using TestData;

    public class ChainAttackPassiveSkill(
        string id)
        : Skill(id)
    {
        public override void Attach(IEntity owner)
        {
            owner.Events.Subscribe<AfterAttackEvent>(OnAfterAttack);
        }

        private void OnAfterAttack(AfterAttackEvent evnt)
        {
            // TODO: How a make sure that other passives already did they part????
            var owner = evnt.Source;

            if (evnt.Context.Rnd.RandFloat() > owner.Parameters.AdditionalHit) return;
            var context = new AttackContext(evnt.Source, evnt.Context.Target, evnt.Source.Parameters.Damage * evnt.Context.Rnd.RandFloatRange(0.9f, 1.1f), new RndGodot(),
                evnt.Context.CombatScheduler);
            context.Schedule();
        }

        public override void Detach(IEntity owner)
        {
            owner.Events.Unsubscribe<AfterAttackEvent>(OnAfterAttack);
        }

        public override ISkill Copy() => new ChainAttackPassiveSkill(Id);

        public override bool IsStronger(ISkill skill) => false;
    }
}

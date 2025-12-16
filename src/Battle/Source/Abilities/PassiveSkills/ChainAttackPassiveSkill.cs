namespace Battle.Source.Abilities.PassiveSkills
{
    using Core.Interfaces.Entity;
    using Core.Interfaces.Events.GameEvents;
    using Core.Interfaces.Skills;
    using TestData;

    public class ChainAttackPassiveSkill(
        string id)
        : Skill(id)
    {
        public override void Attach(IEntity owner)
        {
            Owner = owner;
            owner.CombatEvents.Subscribe<AfterAttackEvent>(OnAfterAttack);
        }

        private void OnAfterAttack(AfterAttackEvent evnt)
        {
            // TODO: How a make sure that other passives already did they part????

            if (Owner == null) return;
            if (evnt.Context.Rnd.RandFloat() > Owner.Parameters.AdditionalHit) return;
            var context = new AttackContext(Owner, evnt.Context.Target, Owner.Parameters.Damage * evnt.Context.Rnd.RandFloatRange(0.9f, 1.1f), new RndGodot(),
                evnt.Context.CombatScheduler);
            context.Schedule();
        }

        public override void Detach(IEntity owner)
        {
            owner.CombatEvents.Unsubscribe<AfterAttackEvent>(OnAfterAttack);
        }

        public override ISkill Copy() => new ChainAttackPassiveSkill(Id);

        public override bool IsStronger(ISkill skill) => false;
    }
}

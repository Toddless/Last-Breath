namespace Battle.Source.Abilities.PassiveSkills
{
    using CombatEvents;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Skills;

    public class HelServantPassiveSkill(string id, float chance) : Skill(id)
    {
        private float Chance { get; } = chance;

        public override void Attach(IEntity owner)
        {
            owner.CombatEvents.Subscribe<AfterAttackEvent>(OnAfterAttack);
        }

        public override void Detach(IEntity owner)
        {
            owner.CombatEvents.Unsubscribe<AfterAttackEvent>(OnAfterAttack);
        }

        private void OnAfterAttack(AfterAttackEvent evnt)
        {
            var context = evnt.Context;
            // TODO: I need to check for target type.Chances for bosses should be lower
            if (context.Rnd.RandFloat() < Chance) context.Target.Kill();
        }

        public override ISkill Copy() => new HelServantPassiveSkill(Id, Chance);

        public override bool IsStronger(ISkill skill)
        {
            if (skill is not HelServantPassiveSkill helServant) return false;

            return Chance > helServant.Chance;
        }
    }
}

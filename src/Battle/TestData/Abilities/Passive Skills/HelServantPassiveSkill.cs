namespace Battle.TestData.Abilities.Passive_Skills
{
    using Core.Enums;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Skills;

    public class HelServantPassiveSkill(string id, float chance, SkillType type) : Skill(id, type)
    {
        private float Chance { get; } = chance;

        public override void Attach(IEntity owner)
        {
            owner.AfterAttack += OnAfterAttack;
        }

        public override void Detach(IEntity owner)
        {
            owner.AfterAttack -= OnAfterAttack;
        }

        private void OnAfterAttack(IAttackContext context)
        {
            // TODO: I need to check for target type.Chances for bosses should be lower
            if (context.Rnd.RandFloat() < Chance) context.Target.Kill();
        }

        public override ISkill Copy() => new HelServantPassiveSkill(Id, Chance, Type);

        public override bool IsStronger(ISkill skill)
        {
            if (skill is not HelServantPassiveSkill helServant) return false;

            return Chance > helServant.Chance;
        }
    }
}

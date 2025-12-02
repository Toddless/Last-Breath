namespace Battle.TestData.Abilities.Passive_Skills
{
    using Core.Enums;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Skills;

    public class RegenerationPassiveSkill(string id, float regenAmount, SkillType type)
        : Skill(id, type)
    {
        private float RegenAmount { get; } = regenAmount;

        public override void Attach(IEntity owner)
        {
            owner.TurnEnd += OnTurnEnd;
            Owner = owner;
        }

        private void OnTurnEnd()
        {
            Owner?.Heal(RegenAmount);
        }

        public override void Detach(IEntity owner)
        {
            owner.TurnEnd -= OnTurnEnd;
            Owner = null;
        }

        public override ISkill Copy() => new RegenerationPassiveSkill(Id, RegenAmount, Type);

        public override bool IsStronger(ISkill skill)
        {
            if (skill is not RegenerationPassiveSkill regenerationPassiveSkill) return false;

            return RegenAmount > regenerationPassiveSkill.RegenAmount;
        }
    }
}

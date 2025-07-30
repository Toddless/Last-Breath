namespace LastBreath.Script.BattleSystem
{
    using LastBreath.Components;
    using LastBreath.Script.Abilities.Interfaces;
    using LastBreath.Script.Enums;

    public class StanceSkillComponent(IStance stance) : BaseSkillComponent<IStanceSkill>()
    {
        private readonly IStance _stance = stance;

        public override void AddSkill(IStanceSkill skill)
        {
            if (skill.RequiredStance != _stance.StanceType)
            {
                //TODO: Log
                return;
            }
            base.AddSkill(skill);
        }

        protected override void ActivateSkill(IStanceSkill skill)
        {
            if (skill.Type != SkillType.AlwaysActive) return;
            skill.Activate(_stance);
        }

        protected override void DeactivateSkill(IStanceSkill skill)
        {
            if (skill.Type != SkillType.AlwaysActive) return;
            skill.Deactivate(_stance);
        }
    }
}

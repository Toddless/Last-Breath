namespace LastBreath.Script.BattleSystem
{
    using Core.Enums;
    using Core.Interfaces.Battle;
    using LastBreath.Components;
    using Utilities;

    public class StanceSkillComponent(IStance stance) : BaseSkillComponent<IStanceSkill>(), IStanceSkillComponent
    {
        private readonly IStance _stance = stance;

        public override void AddSkill(IStanceSkill skill)
        {
            if (skill.RequiredStance != _stance.StanceType)
            {
                Logger.LogInfo($"Trying to add {skill.Id} to stance {_stance.StanceType}", this);
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

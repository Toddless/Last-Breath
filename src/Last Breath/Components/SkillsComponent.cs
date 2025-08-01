namespace LastBreath.Components
{
    using Contracts.Enums;
    using LastBreath.Script;
    using LastBreath.Script.Abilities.Interfaces;

    public class SkillsComponent(ICharacter owner) : BaseSkillComponent<ISkill>()
    {
        private readonly ICharacter _owner = owner;

        protected override void ActivateSkill(ISkill skill)
        {
            if (skill.Type != SkillType.AlwaysActive || skill is not ITargetSkill targetSkill) return;
            targetSkill.Activate(_owner);
        }

        protected override void DeactivateSkill(ISkill skill)
        {
            if (skill.Type != SkillType.AlwaysActive || skill is not ITargetSkill targetSkill) return;
            targetSkill.Deactivate(_owner);
        }
    }
}

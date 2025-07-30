namespace LastBreath.Components
{
    using LastBreath.Script;
    using LastBreath.Script.Abilities.Interfaces;
    using LastBreath.Script.Enums;

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

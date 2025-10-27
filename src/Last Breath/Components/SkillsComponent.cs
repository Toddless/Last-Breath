namespace LastBreath.Components
{
    using Core.Interfaces;
    using Core.Interfaces.Skills;

    public class SkillsComponent(ICharacter owner) : BaseSkillComponent<ISkill>()
    {
        private readonly ICharacter _owner = owner;

        protected override void ActivateSkill(ISkill skill)
        {
          
        }

        protected override void DeactivateSkill(ISkill skill)
        {
   
        }
    }
}

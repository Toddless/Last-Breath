namespace LastBreath.Components
{
    using Core.Interfaces.Entity;
    using Core.Interfaces.Skills;

    public class SkillsComponent(IEntity owner) : BaseSkillComponent<ISkill>()
    {
        private readonly IEntity _owner = owner;

        protected override void ActivateSkill(ISkill skill)
        {
          
        }

        protected override void DeactivateSkill(ISkill skill)
        {
   
        }
    }
}

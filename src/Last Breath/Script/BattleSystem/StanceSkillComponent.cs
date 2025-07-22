namespace Playground.Script.BattleSystem
{
    using Playground.Components;
    using Playground.Script.Abilities.Interfaces;

    public class StanceSkillComponent(IStance stance) : BaseSkillComponent<IStanceSkill>()
    {
        private readonly IStance _stance = stance;

        protected override void ActivateSkill(IStanceSkill skill)
        {
            if (skill.RequiredStance != _stance.StanceType) return;
            skill.Activate(_stance);
        }

        protected override void DeactivateSkill(IStanceSkill skill)
        {
            skill.Deactivate(_stance);
        }
    }
}

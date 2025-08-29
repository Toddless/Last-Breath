namespace LastBreath.Script.BattleSystem.Module
{
    using System.Collections.Generic;
    using Core.Enums;
    using Core.Interfaces.Battle.Module;
    using Core.Interfaces.Skills;
    using LastBreath.Script;

    public abstract class BaseSkillModule(ICharacter owner, SkillType type, DecoratorPriority priority) : ISkillModule
    {
        protected readonly ICharacter Owner = owner;

        public SkillType SkillType { get; } = type;
        public DecoratorPriority Priority { get; } = priority;

        public virtual List<ISkill> GetSkills()
        {
            var skillList = new List<ISkill>();

            skillList.AddRange(Owner.GetSkills(SkillType));

            var stanceSkills = Owner.CurrentStance?.StanceSkillManager.GetSkills(SkillType);
            if (stanceSkills != null && stanceSkills.Count > 0)
                skillList.AddRange(stanceSkills);

            return skillList;
        }
    }
}

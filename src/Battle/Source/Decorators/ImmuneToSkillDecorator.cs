namespace Battle.Source.Decorators
{
    using Core.Enums;
    using System.Linq;
    using Core.Interfaces.Skills;
    using System.Collections.Generic;
    using Core.Interfaces.Battle.Decorator;

    public class ImmuneToSkillDecorator(SkillType type, DecoratorPriority priority, string skillId) : SkillModuleDecorator(type, priority)
    {
        private string _skillIdImmuneTo = skillId;

        public override List<ISkill> GetSkills()
        {
            var skills = base.GetSkills().ToList();
            skills.RemoveAll(skill => skill.Id == _skillIdImmuneTo);
            return skills;
        }
    }
}

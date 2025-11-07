namespace Battle.Source.Decorators
{
    using System.Collections.Generic;
    using System.Linq;
    using Core.Enums;
    using Core.Interfaces.Battle.Decorator;
    using Core.Interfaces.Skills;

    public class ImmuneToSkillTypeDecorator(SkillType type) : SkillModuleDecorator(type, priority: DecoratorPriority.Strong)
    {
        public override List<ISkill> GetSkills()
        {
            var skills = base.GetSkills().ToList();
            skills.RemoveAll(x => x.Type == Parameter);
            return skills;
        }
    }
}

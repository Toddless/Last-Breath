namespace Playground.Script.BattleSystem.Decorators
{
    using System.Collections.Generic;
    using System.Linq;
    using Playground.Script.Abilities.Interfaces;
    using Playground.Script.Enums;

    public class ImmuneToSkillTypeDecorator(SkillType type) : SkillModuleDecorator(type, priority: DecoratorPriority.Strong)
    {
        public override List<ISkill> GetSkills()
        {
            var skills = base.GetSkills().ToList();
            skills.RemoveAll(x => x.Type == SkillType);
            return skills;
        }
    }
}

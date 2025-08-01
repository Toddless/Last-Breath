namespace LastBreath.Script.BattleSystem.Decorators
{
    using System.Collections.Generic;
    using System.Linq;
    using Contracts.Enums;
    using LastBreath.Script.Abilities.Interfaces;

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

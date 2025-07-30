namespace Playground.Script.BattleSystem.Decorators
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Playground.Script.Abilities.Interfaces;
    using Playground.Script.Enums;

    public class ImmuneToSkillDecorator(SkillType type, DecoratorPriority priority, Type skill) : SkillModuleDecorator(type, priority)
    {
        private readonly Type _typeToRemove = skill;

        public override List<ISkill> GetSkills()
        {
            var skills = base.GetSkills().ToList();
            skills.RemoveAll(_typeToRemove.IsInstanceOfType);
            return skills;
        }

        protected override string CreateID() => base.CreateID() + $"_{_typeToRemove.Name}";
    }
}

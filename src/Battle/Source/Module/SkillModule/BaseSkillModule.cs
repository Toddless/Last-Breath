namespace Battle.Source.Module.SkillModule
{
    using Core.Enums;
    using Core.Interfaces.Skills;
    using System.Collections.Generic;
    using Core.Interfaces.Components.Module;
    using Core.Interfaces.Entity;

    public abstract class BaseSkillModule(IEntity owner, SkillType type, DecoratorPriority priority) : ISkillModule
    {
        protected readonly IEntity Owner = owner;

        public SkillType Parameter { get; } = type;
        public DecoratorPriority Priority { get; } = priority;

        public virtual List<ISkill> GetSkills()
        {
            var skillList = new List<ISkill>();

            return skillList;
        }
    }
}

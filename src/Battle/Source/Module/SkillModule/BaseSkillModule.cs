namespace Battle.Source.Module.SkillModule
{
    using System.Collections.Generic;
    using Core.Enums;
    using Core.Interfaces.Components.Module;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Skills;

    public abstract class BaseSkillModule(IFightable owner, SkillType type, Priority priority) : ISkillModule
    {
        protected readonly IFightable Owner = owner;

        public SkillType Parameter { get; } = type;
        public Priority Priority { get; } = priority;

        public virtual List<ISkill> GetSkills()
        {
            var skillList = new List<ISkill>();

            return skillList;
        }
    }
}

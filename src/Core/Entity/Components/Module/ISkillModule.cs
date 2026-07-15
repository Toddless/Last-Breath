namespace Core.Entity.Components.Module
{
    using System.Collections.Generic;
    using Battle.Skills;
    using Enums;

    public interface ISkillModule
    {
        SkillType Parameter { get; }

        Priority Priority { get; }

        List<ISkill> GetSkills();
    }
}

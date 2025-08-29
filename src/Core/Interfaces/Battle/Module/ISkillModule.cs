namespace Core.Interfaces.Battle.Module
{
    using System.Collections.Generic;
    using Core.Enums;
    using Core.Interfaces.Skills;

    public interface ISkillModule
    {
        SkillType SkillType { get; }

        DecoratorPriority Priority { get; }

        List<ISkill> GetSkills();
    }
}

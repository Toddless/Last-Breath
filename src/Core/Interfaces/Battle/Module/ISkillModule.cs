namespace Core.Interfaces.Battle.Module
{
    using Core.Enums;
    using Core.Interfaces.Skills;
    using System.Collections.Generic;

    public interface ISkillModule
    {
        SkillType Parameter { get; }

        DecoratorPriority Priority { get; }

        List<ISkill> GetSkills();
    }
}

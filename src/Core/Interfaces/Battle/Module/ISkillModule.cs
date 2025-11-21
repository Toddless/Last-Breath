namespace Core.Interfaces.Battle.Module
{
    using Enums;
    using Skills;
    using System.Collections.Generic;

    public interface ISkillModule
    {
        SkillType Parameter { get; }

        DecoratorPriority Priority { get; }

        List<ISkill> GetSkills();
    }
}

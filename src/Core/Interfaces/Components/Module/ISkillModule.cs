namespace Core.Interfaces.Components.Module
{
    using System.Collections.Generic;
    using Enums;
    using Skills;

    public interface ISkillModule
    {
        SkillType Parameter { get; }

        DecoratorPriority Priority { get; }

        List<ISkill> GetSkills();
    }
}

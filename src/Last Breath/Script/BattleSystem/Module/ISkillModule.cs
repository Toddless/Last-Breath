namespace Playground.Script.BattleSystem.Module
{
    using System.Collections.Generic;
    using Playground.Script.Abilities.Interfaces;
    using Playground.Script.Enums;

    public interface ISkillModule
    {
        SkillType SkillType { get; }

        DecoratorPriority Priority { get; }

        List<ISkill> GetSkills();
    }
}

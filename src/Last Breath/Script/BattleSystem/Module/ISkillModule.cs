namespace Playground.Script.BattleSystem.Module
{
    using System.Collections.Generic;
    using Playground.Script.Abilities.Interfaces;
    using Playground.Script.Enums;

    public interface ISkillModule
    {
        SkillType Type { get; }

        DecoratorPriority Priority { get; }

        List<ISkill> GetSkills();
    }
}

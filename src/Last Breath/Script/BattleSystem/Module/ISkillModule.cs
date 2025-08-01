namespace LastBreath.Script.BattleSystem.Module
{
    using System.Collections.Generic;
    using Contracts.Enums;
    using LastBreath.Script.Abilities.Interfaces;

    public interface ISkillModule
    {
        SkillType SkillType { get; }

        DecoratorPriority Priority { get; }

        List<ISkill> GetSkills();
    }
}

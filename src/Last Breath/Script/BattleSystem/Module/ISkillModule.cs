namespace LastBreath.Script.BattleSystem.Module
{
    using System.Collections.Generic;
    using Core.Enums;
    using LastBreath.Script.Abilities.Interfaces;

    public interface ISkillModule
    {
        SkillType SkillType { get; }

        DecoratorPriority Priority { get; }

        List<ISkill> GetSkills();
    }
}

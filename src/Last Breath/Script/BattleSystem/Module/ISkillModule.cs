namespace LastBreath.Script.BattleSystem.Module
{
    using System.Collections.Generic;
    using LastBreath.Script.Abilities.Interfaces;
    using LastBreath.Script.Enums;

    public interface ISkillModule
    {
        SkillType SkillType { get; }

        DecoratorPriority Priority { get; }

        List<ISkill> GetSkills();
    }
}

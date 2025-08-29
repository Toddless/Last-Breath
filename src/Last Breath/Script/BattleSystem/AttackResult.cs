namespace LastBreath.Script.BattleSystem
{
    using System.Collections.Generic;
    using Core.Enums;
    using Core.Interfaces.Skills;

    public class AttackResult(List<ISkill> skills, AttackResults result, AttackContext context)
    {
        public List<ISkill> PassiveSkills { get; } = skills;
        public AttackResults Result { get; } = result;
        public AttackContext Context { get; } = context;
    }
}

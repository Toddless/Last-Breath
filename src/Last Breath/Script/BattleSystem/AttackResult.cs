namespace LastBreath.Script.BattleSystem
{
    using System.Collections.Generic;
    using LastBreath.Script.Abilities.Interfaces;
    using LastBreath.Script.Enums;

    public class AttackResult(List<ISkill> skills, AttackResults result, AttackContext context)
    {
        public List<ISkill> PassiveSkills { get; } = skills;
        public AttackResults Result { get; } = result;
        public AttackContext Context { get; } = context;
    }
}

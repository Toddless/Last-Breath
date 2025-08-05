namespace LastBreath.Script.BattleSystem
{
    using System.Collections.Generic;
    using Core.Enums;
    using LastBreath.Script.Abilities.Interfaces;

    public class AttackResult(List<ISkill> skills, AttackResults result, AttackContext context)
    {
        public List<ISkill> PassiveSkills { get; } = skills;
        public AttackResults Result { get; } = result;
        public AttackContext Context { get; } = context;
    }
}

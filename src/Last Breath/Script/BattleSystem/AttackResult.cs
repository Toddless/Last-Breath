namespace Playground.Script.BattleSystem
{
    using System.Collections.Generic;
    using Playground.Script.Abilities.Interfaces;
    using Playground.Script.Enums;

    public class AttackResult(List<ISkill> skills, AttackResults result, AttackContext context)
    {
        public List<ISkill> PassiveSkills { get; } = skills;
        public AttackResults Result { get; } = result;
        public AttackContext Context { get; } = context;
    }
}

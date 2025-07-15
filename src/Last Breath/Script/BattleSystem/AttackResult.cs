namespace Playground.Script.BattleSystem
{
    using System.Collections.Generic;
    using Playground.Script.Abilities.Skills;
    using Playground.Script.Enums;

    public class AttackResult
    {
        public List<ISkill> PassiveSkills { get; }
        public AttackResults Result { get; }
        public AttackContext Context { get; }

        public AttackResult(List<ISkill> skills, AttackResults result, AttackContext context)
        {
            PassiveSkills = skills;
            Result = result;
            Context = context;
        }
    }
}

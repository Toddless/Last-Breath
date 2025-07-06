namespace Playground.Components
{
    using System.Collections.Generic;
    using Playground.Script;
    using Playground.Script.Abilities.Skills;
    using Playground.Script.Enums;

    public class AttackResult
    {
        public List<ISkill> PassiveSkills { get; }
        public AttackResults Result { get; }
        public ICharacter Target { get; }
        public AttackContext Context { get; }

        public AttackResult(List<ISkill> skills, AttackResults result, ICharacter target, AttackContext context)
        {
            PassiveSkills = skills;
            Result = result;
            Target = target;
            Context = context;
        }
    }
}

namespace Playground.Components
{
    using System.Collections.Generic;
    using Playground.Script;
    using Playground.Script.Abilities.Skills;
    using Playground.Script.Enums;

    public class AttackResult
    {
        public List<ISkill> PassiveSkills { get; set; }
        public AttackResults Result { get; set; }
        public ICharacter Target { get; set; }

        public AttackResult(List<ISkill> skills, AttackResults result, ICharacter target)
        {
            PassiveSkills = skills;
            Result = result;
            Target = target;
        }
    }
}

namespace Battle
{
    using System.Collections.Generic;
    using Core.Enums;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Skills;

    public class AttackResult(List<ISkill> skills, AttackResults result, IAttackContext context) : IAttackResult
    {
        public List<ISkill> PassiveSkills { get; } = skills;
        public AttackResults Result { get; } = result;
        public IAttackContext Context { get; } = context;
    }
}

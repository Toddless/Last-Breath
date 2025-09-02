namespace Core.Interfaces.Battle
{
    using System.Collections.Generic;
    using Core.Enums;
    using Core.Interfaces.Skills;

    public interface IAttackResult
    {
        IAttackContext Context { get; }
        List<ISkill> PassiveSkills { get; }
        AttackResults Result { get; }
    }
}

namespace Playground.Script.BattleSystem.Module
{
    using System.Collections.Generic;
    using Playground.Script.Abilities.Interfaces;
    using Playground.Script.Enums;

    public abstract class BaseSkillModule(ICharacter owner, SkillType type, DecoratorPriority priority) : ISkillModule
    {
        protected readonly ICharacter Owner = owner;
        public SkillType Type { get; } = type;
        public DecoratorPriority Priority { get; } = priority;
        public virtual List<ISkill> GetSkills() => Owner.GetSkills(Type);
    }
}

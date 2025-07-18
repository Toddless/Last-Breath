namespace Playground.Script.BattleSystem.Decorators
{
    using System.Collections.Generic;
    using Playground.Script.Abilities.Interfaces;
    using Playground.Script.BattleSystem.Module;
    using Playground.Script.Enums;

    public abstract class SkillModuleDecorator(SkillModule type, DecoratorPriority priority) : ISkillModule
    {
        private ISkillModule? _module;

        public SkillModule ModuleType { get; } = type;

        public DecoratorPriority Priority { get; } = priority;

        public void ChainModule(ISkillModule module) => _module = module;

        public virtual List<ISkill> GetSkills() => _module?.GetSkills() ?? [];
    }
}

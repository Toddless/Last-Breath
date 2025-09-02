namespace Core.Interfaces.Battle.Decorator
{
    using System;
    using System.Collections.Generic;
    using Core.Enums;
    using Core.Interfaces.Battle.Module;
    using Core.Interfaces.Skills;

    public abstract class SkillModuleDecorator : ISkillModule, IModuleDecorator<SkillType, ISkillModule>
    {
        private ISkillModule? _module;
        private readonly Lazy<string> _id;

        public SkillType SkillType { get; }

        public DecoratorPriority Priority { get; }

        public string Id => _id.Value;

        public SkillModuleDecorator(SkillType type, DecoratorPriority priority)
        {
            SkillType = type;
            Priority = priority;
            _id = new(CreateID);
        }

        public void ChainModule(ISkillModule module) => _module = module;

        public virtual List<ISkill> GetSkills() => _module?.GetSkills() ?? [];

        protected virtual string CreateID() => $"{GetType().Name}_{SkillType}_{Priority}";
    }
}

namespace Core.Interfaces.Battle.Decorator
{
    using Enums;
    using System;
    using Skills;
    using Module;
    using System.Collections.Generic;

    public abstract class SkillModuleDecorator : ISkillModule, IModuleDecorator<SkillType, ISkillModule>
    {
        private ISkillModule? _module;
        private readonly Lazy<string> _id;

        public SkillType Parameter { get; }

        public DecoratorPriority Priority { get; }

        public string Id => _id.Value;

        protected SkillModuleDecorator(SkillType type, DecoratorPriority priority)
        {
            Parameter = type;
            Priority = priority;
            _id = new(CreateID);
        }

        public void ChainModule(ISkillModule module) => _module = module;

        public virtual List<ISkill> GetSkills() => _module?.GetSkills() ?? [];

        protected virtual string CreateID() => $"{GetType().Name}_{Parameter}_{Priority}";
    }
}

namespace LastBreath.Script.BattleSystem.Decorators
{
    using System;
    using Contracts.Enums;
    using LastBreath.Script.BattleSystem.Module;

    public abstract class StatModuleDecorator : IStatModule, IModuleDecorator<StatModule, IStatModule>
    {
        private IStatModule? _module;
        private readonly Lazy<string> _id;

        public StatModule SkillType { get; }
        public DecoratorPriority Priority { get; }
        public string Id => _id.Value;

        public StatModuleDecorator(StatModule type, DecoratorPriority priority)
        {
            SkillType = type;
            Priority = priority;
            _id = new(CreateID);
        }

        public void ChainModule(IStatModule module) => _module = module;

        public virtual float GetValue() => _module?.GetValue() ?? 0;

        protected virtual string CreateID() => $"{GetType().Name}_{SkillType}_{Priority}";
    }
}

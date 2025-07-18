namespace Playground.Components
{
    using System;
    using System.Linq;
    using Playground.Script;
    using Playground.Script.Enums;
    using System.Collections.Generic;
    using Playground.Script.BattleSystem.Module;
    using Playground.Script.BattleSystem.Decorators;

    public class SkillModuleDecoratorManager : BaseModuleManager<SkillModule, ISkillModule>
    {
        private readonly Dictionary<SkillModule, List<SkillModuleDecorator>> _moduleDecorators;
        private readonly Dictionary<SkillModule, ISkillModule> _baseModule;

        public SkillModuleDecoratorManager(ICharacter owner) : base([], owner)
        {
            _moduleDecorators = Enum.GetValues<SkillModule>().ToDictionary(param => param, _ => new List<SkillModuleDecorator>());
            _baseModule = new Dictionary<SkillModule, ISkillModule>
            {
                [SkillModule.PreAttack] = new PreAttackSkillModule(Owner),
                [SkillModule.OnAttack] = new OnAttackSkillModule(Owner),
                [SkillModule.AlwaysActive] = new AlwaysActiveSkillModule(Owner),
            };
        }

        public override ISkillModule GetModule(SkillModule parameter)
        {
            if (Cache.TryGetValue(parameter, out var value)) return value;

            var tmpModule = GetBaseModule(parameter);
            // Strong decorators should be placed at the end of a chain.
            foreach (var module in _moduleDecorators[parameter])
            {
                module.ChainModule(tmpModule);
                tmpModule = module;
            }
            Cache[parameter] = tmpModule;
            return tmpModule;
        }


        public void AddDecorator(SkillModuleDecorator decorator)
        {
            var decorators = _moduleDecorators[decorator.ModuleType];
            // We cant add the same decorator twice
            if (AlreadyHaveThisTypeOfDecorator(decorators, decorator))
            {
                // TODO: Log? Event?
                return;
            }

            int index = decorators.FindIndex(x => x.Priority > decorator.Priority);
            if (index < 0)
                decorators.Add(decorator);
            else
                decorators.Insert(index, decorator);

            RaiseModuleDecoratorChanges(decorator.ModuleType);
        }

        public void RemodeDecorator(SkillModuleDecorator decorator)
        {
            // just in case
            if (!_moduleDecorators.TryGetValue(decorator.ModuleType, out var decorators))
            {
                // TODO: Log
                return;
            }
            if (decorators.Remove(decorator))
                RaiseModuleDecoratorChanges(decorator.ModuleType);
        }

        protected override ISkillModule GetBaseModule(SkillModule key) => _baseModule[key];

        private bool AlreadyHaveThisTypeOfDecorator(IEnumerable<SkillModuleDecorator> decorators, SkillModuleDecorator decorator) => decorators.Any(x => x.GetType() == decorator.GetType());
    }
}

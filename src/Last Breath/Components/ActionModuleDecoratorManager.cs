namespace Playground.Components
{
    using System;
    using System.Linq;
    using Playground.Script;
    using Playground.Script.Enums;
    using System.Collections.Generic;
    using Playground.Script.BattleSystem.Module;
    using Playground.Script.BattleSystem.Decorators;

    public class ActionModuleDecoratorManager : BaseModuleManager<ActionModule, IActionModule<ICharacter>>
    {
        private readonly Dictionary<ActionModule, List<ActionModuleDecorator>> _moduleDecorators;
        private readonly Dictionary<ActionModule, IActionModule<ICharacter>> _baseModule;

        public ActionModuleDecoratorManager(ICharacter owner): base([], owner)
        {
            // TODO: Same code twice. I'll change it later.
            _moduleDecorators = Enum.GetValues<ActionModule>().ToDictionary(type => type, _ => new List<ActionModuleDecorator>());
            // TODO: Factory
            _baseModule = new Dictionary<ActionModule, IActionModule<ICharacter>>
            {
                [ActionModule.EvadeAction] = new HandleAttackEvadeModule(Owner),
                [ActionModule.SucceedAction] = new HandleAttackSucceedModule(Owner),
                [ActionModule.BlockAction] = new HandleAttackBlockedModule(Owner),
            };
        }

        public override IActionModule<ICharacter> GetModule(ActionModule type)
        {
            if (Cache.TryGetValue(type, out var value)) return value;

            var tmpModule = GetBaseModule(type);

            foreach (var module in _moduleDecorators[type])
            {
                module.ChainModule(tmpModule);
                tmpModule = module;
            }
            Cache[type] = tmpModule;
            return tmpModule;
        }

        public void AddDecoratorToModule(ActionModuleDecorator decorator)
        {
            var decorators = _moduleDecorators[decorator.ModuleType];

            if (AlreadyHaveThisTypeOfDecorator(decorators, decorator))
                return;

            int index = decorators.FindIndex(x => x.Priority > decorator.Priority);
            if (index < 0)
                decorators.Add(decorator);
            else
                decorators.Insert(index, decorator);

            RaiseModuleDecoratorChanges(decorator.ModuleType);
        }

        public void RemoveDecoratorFromModule(ActionModuleDecorator decorator)
        {
            if (!_moduleDecorators.TryGetValue(decorator.ModuleType, out var decorators))
            {
                // TODO: Log
                return;
            }
            if (decorators.Remove(decorator))
                RaiseModuleDecoratorChanges(decorator.ModuleType);
        }

        protected override IActionModule<ICharacter> GetBaseModule(ActionModule type) => _baseModule[type];

        private bool AlreadyHaveThisTypeOfDecorator(IEnumerable<ActionModuleDecorator> decorators, ActionModuleDecorator decorator) => decorators.Any(x => x.GetType() == decorator.GetType());
    }
}

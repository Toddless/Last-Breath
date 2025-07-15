namespace Playground.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Playground.Script;
    using Playground.Script.BattleSystem.Decorators;
    using Playground.Script.BattleSystem.Module;
    using Playground.Script.Enums;

    public class ActionModuleDecoratorManager 
    {
        private readonly Dictionary<ActionModuleType, List<ActionModuleDecorator>> _moduleDecorators;
        private readonly HandleAttackBlockedModule _blockedModule;
        private readonly HandleAttackEvadeModule _evadeModule;
        private readonly HandleAttackSucceedModule _succeedModule;
        private readonly ICharacter _owner;

        public event Action<ActionModuleType, IActionModule<ICharacter>>? ModuleDecoratorChanges;

        public ActionModuleDecoratorManager(ICharacter owner)
        {
            // TODO: Same code twice. I'll change it later.
            _owner = owner;
            _moduleDecorators = Enum.GetValues<ActionModuleType>().ToDictionary(type => type, _ => new List<ActionModuleDecorator>());
            _blockedModule = new(_owner);
            _evadeModule = new(_owner);
            _succeedModule = new(_owner);
        }

        public IActionModule<ICharacter> GetModule(ActionModuleType type)
        {
            var tempModule = GetBaseModule(type);

            foreach (var module in _moduleDecorators[type])
            {
                module.ChainModule(tempModule);
                tempModule = module;
            }

            return tempModule;
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


        private IActionModule<ICharacter> GetBaseModule(ActionModuleType type)
        {
            return type switch
            {
                ActionModuleType.EvadeAction => _evadeModule,
                ActionModuleType.BlockAction => _blockedModule,
                ActionModuleType.SucceedAction => _succeedModule,
                _ => throw new ArgumentOutOfRangeException(nameof(type)),
            };
        }

        private void RaiseModuleDecoratorChanges(ActionModuleType type) => ModuleDecoratorChanges?.Invoke(type, GetModule(type));
        private bool AlreadyHaveThisTypeOfDecorator(IEnumerable<ActionModuleDecorator> decorators, ActionModuleDecorator decorator) => decorators.Any(x => x.GetType() == decorator.GetType());
    }
}

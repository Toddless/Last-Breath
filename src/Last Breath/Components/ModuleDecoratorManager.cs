namespace Playground.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Playground.Script;
    using Playground.Script.BattleSystem.Decorators;
    using Playground.Script.BattleSystem.Module;
    using Playground.Script.Enums;

    public class ModuleDecoratorManager
    {
        private readonly Dictionary<ModuleParameter, List<ModuleDecorator>> _moduleDecorators;
        private readonly AdditionalHitChanceModule _additionalHitChanceModule;
        private readonly BlockChanceModule _blockChanceModule;
        private readonly EvadeChanceModule _evadeChanceModule;
        private readonly CritChanceModule _critChanceModule;
        private readonly CritDamageModule _critDamageModule;
        private readonly DamageModule _damageModule;
        private readonly ICharacter _owner;

        public event Action<ModuleParameter, IModule>? ModuleDecoratorChanges;

        public ModuleDecoratorManager(ICharacter owner)
        {
            _owner = owner;
            _moduleDecorators = Enum.GetValues<ModuleParameter>().ToDictionary(param => param, _ => new List<ModuleDecorator>());
            _critChanceModule = new();
            _evadeChanceModule = new();
            _additionalHitChanceModule = new();
            _blockChanceModule = new();
            _damageModule = new(_owner);
            _critDamageModule = new(_owner);
        }

        public IModule GetModule(ModuleParameter parameter)
        {
            var tmpModule = GetBaseModule(parameter);

            // Strong decorators should be placed at the end of a chain.
            foreach (var module in _moduleDecorators[parameter])
            {
                module.ChainModule(tmpModule);
                tmpModule = module;
            }

            return tmpModule;
        }

        public void AddDecoratorToModule(ModuleDecorator decorator, ModuleParameter parameter)
        {
            var decorators = _moduleDecorators[parameter];
            // We cant add same decorator twice
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

            RaiseModuleDecoratorChanges(parameter);
        }


        public void RemoveDecoratorFromModule(ModuleDecorator decorator, ModuleParameter parameter)
        {
            // just in case
            if (!_moduleDecorators.TryGetValue(parameter, out var decorators))
            {
                // TODO: Log
                return;
            }
            if (decorators.Remove(decorator))
                RaiseModuleDecoratorChanges(parameter);
        }

        private IModule GetBaseModule(ModuleParameter parameter)
        {
            return parameter switch
            {
                ModuleParameter.CritChance => _critChanceModule,
                ModuleParameter.Damage => _damageModule,
                ModuleParameter.CritDamage => _critDamageModule,
                ModuleParameter.EvadeChance => _evadeChanceModule,
                ModuleParameter.AdditionalAttackChance => _additionalHitChanceModule,
                ModuleParameter.BlockChance => _blockChanceModule,
                _ => throw new ArgumentOutOfRangeException(nameof(parameter))
            };
        }

        private void RaiseModuleDecoratorChanges(ModuleParameter parameter) => ModuleDecoratorChanges?.Invoke(parameter, GetModule(parameter));
        private bool AlreadyHaveThisTypeOfDecorator(IEnumerable<ModuleDecorator> decorators, ModuleDecorator decorator) => decorators.Any(x => x.GetType() == decorator.GetType());
    }
}

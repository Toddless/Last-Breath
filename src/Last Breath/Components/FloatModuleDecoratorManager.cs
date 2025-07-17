namespace Playground.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Playground.Script;
    using Playground.Script.BattleSystem.Decorators;
    using Playground.Script.BattleSystem.Module;
    using Playground.Script.Enums;

    public class FloatModuleDecoratorManager
    {
        private readonly Dictionary<ModuleParameter, List<FloatModuleDecorator>> _moduleDecorators;
        private readonly Dictionary<ModuleParameter, IValueModule<float>> _cache = [];
        private readonly AdditionalHitChanceModule _additionalHitChanceModule;
        private readonly BlockChanceModule _blockChanceModule;
        private readonly EvadeChanceModule _evadeChanceModule;
        private readonly CritChanceModule _critChanceModule;
        private readonly CritDamageModule _critDamageModule;
        private readonly DamageModule _damageModule;
        private readonly ArmorModule _armorModule;
        private readonly MaxReduceDamageModule _reduceDamageModule;
        private readonly ICharacter _owner;

        public event Action<ModuleParameter, IValueModule<float>>? ModuleDecoratorChanges;

        public FloatModuleDecoratorManager(ICharacter owner)
        {
            // TODO: Same code twice. I'll change it later.
            _owner = owner;
            _moduleDecorators = Enum.GetValues<ModuleParameter>().ToDictionary(param => param, _ => new List<FloatModuleDecorator>());
            _critChanceModule = new();
            _evadeChanceModule = new();
            _additionalHitChanceModule = new();
            _blockChanceModule = new();
            _armorModule = new(_owner);
            _reduceDamageModule = new(_owner);
            _damageModule = new(_owner);
            _critDamageModule = new(_owner);
        }

        public IValueModule<float> GetModule(ModuleParameter parameter)
        {
            if(_cache.TryGetValue(parameter, out var value)) return value;

            var tmpModule = GetBaseModule(parameter);
            // Strong decorators should be placed at the end of a chain.
            foreach (var module in _moduleDecorators[parameter])
            {
                module.ChainModule(tmpModule);
                tmpModule = module;
            }
            _cache[parameter] = tmpModule;
            return tmpModule;
        }

        public void AddDecoratorToModule(FloatModuleDecorator decorator)
        {
            var decorators = _moduleDecorators[decorator.Parameter];
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

            RaiseModuleDecoratorChanges(decorator.Parameter);
        }


        public void RemoveDecoratorFromModule(FloatModuleDecorator decorator)
        {
            // just in case
            if (!_moduleDecorators.TryGetValue(decorator.Parameter , out var decorators))
            {
                // TODO: Log
                return;
            }
            if (decorators.Remove(decorator))
                RaiseModuleDecoratorChanges(decorator.Parameter);
        }

        private IValueModule<float> GetBaseModule(ModuleParameter parameter)
        {
            return parameter switch
            {
                ModuleParameter.CritChance => _critChanceModule,
                ModuleParameter.Damage => _damageModule,
                ModuleParameter.CritDamage => _critDamageModule,
                ModuleParameter.EvadeChance => _evadeChanceModule,
                ModuleParameter.AdditionalAttackChance => _additionalHitChanceModule,
                ModuleParameter.BlockChance => _blockChanceModule,
                ModuleParameter.Armor => _armorModule,
                ModuleParameter.MaxReduceDamage => _reduceDamageModule,
                _ => throw new ArgumentOutOfRangeException(nameof(parameter))
            };
        }

        private void RaiseModuleDecoratorChanges(ModuleParameter parameter)
        {
            _cache.Remove(parameter);
            ModuleDecoratorChanges?.Invoke(parameter, GetModule(parameter));
        }
        private static bool AlreadyHaveThisTypeOfDecorator(IEnumerable<FloatModuleDecorator> decorators, FloatModuleDecorator decorator) => decorators.Any(x => x.GetType() == decorator.GetType());
    }
}

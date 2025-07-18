namespace Playground.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Playground.Script;
    using Playground.Script.BattleSystem.Decorators;
    using Playground.Script.BattleSystem.Module;
    using Playground.Script.Enums;

    public class StatModuleDecoratorManager : BaseModuleManager<StatModule, IStatModule>
    {
        private readonly Dictionary<StatModule, List<StatModuleDecorator>> _moduleDecorators;
        private readonly Dictionary<StatModule, IStatModule> _baseModule;

        public StatModuleDecoratorManager(ICharacter owner) : base([], owner)
        {
            // TODO: Same code twice. I'll change it later.
            _moduleDecorators = Enum.GetValues<StatModule>().ToDictionary(param => param, _ => new List<StatModuleDecorator>());
            // TODO: Factory?? 
            _baseModule = new Dictionary<StatModule, IStatModule>
            {
                [StatModule.AdditionalAttackChance] = new AdditionalHitChanceModule(),
                [StatModule.BlockChance] = new BlockChanceModule(),
                [StatModule.CritChance] = new CritChanceModule(),
                [StatModule.EvadeChance] = new EvadeChanceModule(),
                [StatModule.Damage] = new DamageModule(Owner),
                [StatModule.Armor] = new ArmorModule(Owner),
                [StatModule.MaxReduceDamage] = new MaxReduceDamageModule(Owner),
                [StatModule.CritDamage] = new CritDamageModule(Owner),
            };
        }

        public override IStatModule GetModule(StatModule parameter)
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

        public void AddDecorator(StatModuleDecorator decorator)
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


        public void RemoveDecorator(StatModuleDecorator decorator)
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

        protected override IStatModule GetBaseModule(StatModule parameter) => _baseModule[parameter];

        private static bool AlreadyHaveThisTypeOfDecorator(IEnumerable<StatModuleDecorator> decorators, StatModuleDecorator decorator) => decorators.Any(x => x.GetType() == decorator.GetType());
    }
}

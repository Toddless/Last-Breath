namespace Playground.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Playground.Script.BattleSystem.Decorators;

    public class ModuleManager<TKey, TModule, TDecorator>
        where TKey : notnull
        where TModule : class
        where TDecorator : TModule, IModuleDecorator<TKey, TModule>
    {
        private readonly Dictionary<TKey, TModule> _baseModules;
        private readonly Dictionary<TKey, List<TDecorator>> _decorators;
        private readonly Dictionary<TKey, TModule> _cache = [];

        public event Action<TKey, TModule>? ModuleDecoratorChanges;

        public ModuleManager(Dictionary<TKey, TModule> baseModule)
        {
            _baseModules = baseModule;

            if (typeof(TKey).IsEnum)
            {
                _decorators = Enum.GetValues(typeof(TKey)).Cast<TKey>().ToDictionary(key => key, _ => new List<TDecorator>());
            }
            else
            {
                _decorators = [];
            }
        }

        public TModule GetModule(TKey key)
        {
            if (_cache.TryGetValue(key, out var cachedModule)) return cachedModule;

            var tmpModule = _baseModules[key];

            foreach (var module in _decorators[key])
            {
                module.ChainModule(tmpModule);
                tmpModule = module;
            }

            _cache[key] = tmpModule;
            return tmpModule;
        }

        public void AddDecorator(TDecorator decorator)
        {
            if (!_decorators.TryGetValue(decorator.Type, out var list))
            {
                list = [];
                _decorators[decorator.Type] = list;
            }

            // Check if this type of decorator already exists
            if (list.Any(d => d.GetType() == decorator.GetType() && d.Priority == decorator.Priority)) return;

            var idx = list.FindIndex(oldDecorator => oldDecorator.Priority > decorator.Priority);
            if (idx < 0) list.Add(decorator);
            else list.Insert(idx, decorator);

            RaiseModuleDecoratorChanges(decorator.Type);
        }

        public void RemoveDecorator(TDecorator decorator)
        {
            // just in case
            if (!_decorators.TryGetValue(decorator.Type, out var decorators))
            {
                // TODO: Log
                return;
            }
            if (decorators.Remove(decorator))
                RaiseModuleDecoratorChanges(decorator.Type);
        }


        private void RaiseModuleDecoratorChanges(TKey key)
        {
            _cache.Remove(key);
            ModuleDecoratorChanges?.Invoke(key, GetModule(key));
        }
    }
}

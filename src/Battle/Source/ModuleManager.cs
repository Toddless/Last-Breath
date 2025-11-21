namespace Battle.Source
{
    using System;
    using Utilities;
    using System.Linq;
    using System.Collections.Generic;
    using Core.Interfaces.Components;
    using Core.Interfaces.Battle.Decorator;

    public class ModuleManager<TKey, TModule, TDecorator> : IModuleManager<TKey, TModule, TDecorator>
        where TKey : struct, Enum
        where TModule : class
        where TDecorator : TModule, IModuleDecorator<TKey, TModule>
    {
        private readonly Dictionary<TKey, TModule> _baseModules;
        private readonly Dictionary<TKey, List<TDecorator>> _decorators;
        private readonly Dictionary<TKey, TModule> _cache = [];

        public ModuleManager(Dictionary<TKey, TModule> baseModule)
        {
            _baseModules = baseModule;
            _decorators = Enum.GetValues<TKey>().ToDictionary(key => key, _ => new List<TDecorator>());
        }

        public event Action<TKey>? ModuleChanges;

        public TModule GetModule(TKey key)
        {
            if (_cache.TryGetValue(key, out var cachedModule)) return cachedModule;

            TModule tmpModule = _baseModules[key];

            foreach (var decorator in _decorators[key])
            {
                decorator.ChainModule(tmpModule);
                tmpModule = decorator;
            }

            _cache[key] = tmpModule;
            return tmpModule;
        }

        public void AddDecorator(TDecorator decorator)
        {
            if (!_decorators.TryGetValue(decorator.Parameter, out var list))
            {
                Tracker.TrackNotFound($"List for {decorator.Parameter}", this);
                list = [];
                _decorators[decorator.Parameter] = list;
            }

            // Check if this type of decorator already in list
            if (list.Any(x => x.Id == decorator.Id && x.Priority == decorator.Priority)) return;
            list.Add(decorator);
            list.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            RaiseModuleChanges(decorator.Parameter);
        }

        public void RemoveDecorator(string decoratorId, TKey key)
        {
            // just in case
            if (!_decorators.TryGetValue(key, out var decorators))
            {
                Tracker.TrackNotFound("Trying to remove from non-existent list", this);
                return;
            }

            var exist = decorators.FirstOrDefault(x => x.Id == decoratorId);

            if (exist == null) return;
            decorators.Remove(exist);
            RaiseModuleChanges(key);
        }

        private void RaiseModuleChanges(TKey key)
        {
            _cache.Remove(key);
            ModuleChanges?.Invoke(key);
        }
    }
}

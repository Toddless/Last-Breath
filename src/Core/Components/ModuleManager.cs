namespace Core.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Decorator;

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

        public IReadOnlyCollection<TKey> Keys => _baseModules.Keys;

        public bool AddBaseModule(TKey key, TModule module) => _baseModules.TryAdd(key, module);

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

        public void AddDecorator(TDecorator newDecorator)
        {
            if (!_decorators.TryGetValue(newDecorator.Parameter, out var list))
            {
                Tracker.TrackNotFound($"List for {newDecorator.Parameter}", this);
                list = [];
                _decorators[newDecorator.Parameter] = list;
            }

            var existing = list.FirstOrDefault(decorator => decorator.Id == newDecorator.Id);
            if (existing != null)
            {
                if (existing.IsStronger(newDecorator)) return;
                list.Remove(existing);
            }

            list.Add(newDecorator);
            list.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            RaiseModuleChanges(newDecorator.Parameter);
        }

        public void RemoveDecorator(string decoratorId, TKey key)
        {
            // just in case
            if (!_decorators.TryGetValue(key, out var decorators))
            {
                Tracker.TrackNotFound("Trying to remove from non-existent list", this);
                return;
            }

            var exist = decorators.FirstOrDefault(decorator => decorator.Id == decoratorId);

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

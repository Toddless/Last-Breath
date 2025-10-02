namespace LastBreath.Components
{
    using System;
    using Utilities;
    using System.Linq;
    using System.Collections.Generic;
    using Core.Interfaces.Components;
    using Core.Interfaces.Battle.Decorator;

    public class ModuleManager<TKey, TModule, TDecorator> : IModuleManager<TKey, TModule, TDecorator>
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
            if (!_decorators.TryGetValue(decorator.SkillType, out var list))
            {
                Tracker.TrackNotFound($"List for {decorator.SkillType}", this);
                list = [];
                _decorators[decorator.SkillType] = list;
            }

            // Check if this type of decorator already in list
            if (list.Any(x => x.Id == decorator.Id)) return;

            var idx = list.FindIndex(oldDecorator => oldDecorator.Priority > decorator.Priority);
            if (idx < 0) list.Add(decorator);
            else list.Insert(idx, decorator);

            RaiseModuleDecoratorChanges(decorator.SkillType);
        }

        public void RemoveDecorator(TDecorator decorator)
        {
            // just in case
            if (!_decorators.TryGetValue(decorator.SkillType, out var decorators))
            {
                Tracker.TrackNotFound("Trying to remove from non-existent list", this);
                return;
            }
            if (decorators.Remove(decorator))
                RaiseModuleDecoratorChanges(decorator.SkillType);
        }


        private void RaiseModuleDecoratorChanges(TKey key)
        {
            _cache.Remove(key);
            ModuleDecoratorChanges?.Invoke(key, GetModule(key));
        }
    }
}

namespace Playground.Components
{
    using System;
    using System.Collections.Generic;
    using Playground.Script;

    public abstract class BaseModuleManager<TKey, TModule>(Dictionary<TKey, TModule> cache, ICharacter owner)
        where TKey : notnull
    {
        protected Dictionary<TKey, TModule> Cache = cache;
        protected readonly ICharacter Owner = owner;

        public event Action<TKey, TModule>? ModuleDecoratorChanges;

       // Due to the way the decoration works, I can't create generic logic here. If I find a way, I'll change it.
        public abstract TModule GetModule(TKey key);

        protected abstract TModule GetBaseModule(TKey key);

        protected void RaiseModuleDecoratorChanges(TKey key)
        {
            Cache.Remove(key);
            ModuleDecoratorChanges?.Invoke(key, GetModule(key));
        }
    }
}

namespace Core.Entity.Components
{
    using System;
    using System.Collections.Generic;
    using Decorator;

    public interface IModuleManager<TKey, TModule, TDecorator>
        where TKey : struct, Enum
        where TModule : class
        where TDecorator : IModuleDecorator<TKey, TModule>
    {
        event Action<TKey>? ModuleChanges;

        /// <summary>Keys with a base module — the ability's own parameter set (description values are built from it).</summary>
        IReadOnlyCollection<TKey> Keys { get; }

        TModule GetModule(TKey key);
        bool AddBaseModule(TKey key, TModule module);
        void AddDecorator(TDecorator newDecorator);
        void RemoveDecorator(string decoratorId, TKey key);
    }
}

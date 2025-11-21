namespace Core.Interfaces.Components
{
    using System;
    using Battle.Decorator;

    public interface IModuleManager<TKey, out TModule, in TDecorator>
        where TKey : struct, Enum
        where TModule : class
        where TDecorator : IModuleDecorator<TKey, TModule>
    {
        event Action<TKey>? ModuleChanges;

        TModule GetModule(TKey key);
        void AddDecorator(TDecorator decorator);
        void RemoveDecorator(string decoratorId, TKey key);
    }
}

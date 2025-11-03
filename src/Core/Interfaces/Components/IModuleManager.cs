namespace Core.Interfaces.Components
{
    using System;
    using Core.Interfaces.Battle.Decorator;

    public interface IModuleManager<TKey, TModule, TDecorator>
        where TKey : notnull
        where TModule : class
        where TDecorator : IModuleDecorator<TKey, TModule>, TModule
    {
        event Action<TKey, TModule>? ModuleDecoratorChanges;

        TModule GetModule(TKey key);
        void AddDecorator(TDecorator decorator);
        void RemoveDecorator(TDecorator decorator);
    }
}

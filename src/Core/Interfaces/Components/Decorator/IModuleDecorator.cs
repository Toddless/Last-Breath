namespace Core.Interfaces.Components.Decorator
{
    using System;
    using Enums;

    public interface IModuleDecorator<TKey, TModule>
        where TKey : struct, Enum
    {
        string Id { get; }
        TKey Parameter { get; }
        Priority Priority { get; }

        void ChainModule(TModule inner);
        bool IsStronger(IModuleDecorator<TKey, TModule> decorator);
    }
}

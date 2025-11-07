namespace Core.Interfaces.Battle.Decorator
{
    using System;
    using Core.Enums;

    public interface IModuleDecorator<TKey, TModule>
        where TKey : struct, Enum
    {
        string Id { get; }
        TKey Parameter { get; }
        DecoratorPriority Priority { get; }

        void ChainModule(TModule inner);
    }
}

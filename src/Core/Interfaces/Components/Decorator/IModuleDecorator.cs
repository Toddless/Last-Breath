namespace Core.Interfaces.Components.Decorator
{
    using System;
    using Enums;

    public interface IModuleDecorator<out TKey, in TModule>
        where TKey : struct, Enum
    {
        string Id { get; }
        TKey Parameter { get; }
        DecoratorPriority Priority { get; }

        void ChainModule(TModule inner);
    }
}

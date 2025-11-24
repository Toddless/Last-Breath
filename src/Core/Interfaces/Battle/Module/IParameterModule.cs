namespace Core.Interfaces.Battle.Module
{
    using System;
    using Enums;

    public interface IParameterModule<out TKey>
        where TKey : struct, Enum
    {
        TKey Parameter { get; }
        DecoratorPriority Priority { get; }

        float GetValue();
        float ApplyDecoratorsForValue(float value);
    }
}

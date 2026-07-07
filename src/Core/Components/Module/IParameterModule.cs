namespace Core.Components.Module
{
    using System;
    using Enums;

    public interface IParameterModule<out TKey>
        where TKey : struct, Enum
    {
        TKey Parameter { get; }
        Priority Priority { get; }

        float GetValue();
        float ApplyDecoratorsForValue(float applyToValue);
    }
}

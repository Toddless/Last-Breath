namespace Battle.Source.Module
{
    using System;
    using Core.Enums;
    using Core.Interfaces.Battle.Module;

    public class Module<TKey>(Func<float> value, TKey parameter) : IParameterModule<TKey>
        where TKey : struct, Enum
    {
        public TKey Parameter { get; } = parameter;
        public DecoratorPriority Priority => DecoratorPriority.Base;
        public float GetValue() => value();
        public float ApplyDecorators(float baseBalue) => baseBalue;
    }
}

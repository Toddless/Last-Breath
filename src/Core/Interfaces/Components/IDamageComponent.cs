namespace Core.Interfaces.Components
{
    using System;
    using Core.Enums;

    public interface IDamageComponent
    {
        float AdditionalHit { get; }
        float CriticalChance { get; }
        float CriticalDamage { get; }
        float Damage { get; }
        float MaxAdditionalHit { get; }
        float MaxCriticalChance { get; }

        event Action<Parameter, float>? PropertyValueChanges;

        void AddOverrideFuncForParameter(Func<float, IModifiersChangedEventArgs, float> newFunc, Parameter parameter);
        void ChangeStrategy(IDamageStrategy strategy);
        void OnParameterChanges(object? sender, IModifiersChangedEventArgs args);
        void RemoveOverrideFuncForParameter(Parameter parameter);
    }
}

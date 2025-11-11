namespace Core.Interfaces.Components
{
    using System;
    using Core.Enums;
    using Core.Interfaces.Battle.Decorator;

    public interface IHealthComponent
    {
        float CurrentHealth { get; }
        float MaxHealth { get; }
        float HealthRecovery { get; }

        event Action<float>? CurrentHealthChanged;
        event Action? NoMoreHealth;
        event Action<Parameter, float> ParameterChanged;

        void RemoveModuleDecorator(string id, Parameter key);
        void AddModuleDecorator(StatModuleDecorator decorator);
        float CurrentHealthPercent();
        void Heal(float amount);
        void HealUpToMax();
        void OnParameterChanges(object? sender, IModifiersChangedEventArgs args);
        void TakeDamage(float damage);
    }
}

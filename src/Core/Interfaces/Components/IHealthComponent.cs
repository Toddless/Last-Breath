namespace Core.Interfaces.Components
{
    using System;
    using Decorator;
    using Enums;

    public interface IHealthComponent
    {
        float CurrentHealth { get; }
        float MaxHealth { get; }
        float HealthRecovery { get; }

        event Action<float>? CurrentHealthChanged;
        event Action? NoMoreHealth;
        event Action<EntityParameter, float> ParameterChanged;

        void RemoveModuleDecorator(string id, EntityParameter key);
        void AddModuleDecorator(EntityParameterModuleDecorator decorator);
        float CurrentHealthPercent();
        void Heal(float amount);
        void HealUpToMax();
        void OnParameterChanges(object? sender, IModifiersChangedEventArgs args);
        void TakeDamage(float damage);
    }
}

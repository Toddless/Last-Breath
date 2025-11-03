namespace Core.Interfaces.Components
{
    using System;

    public interface IHealthComponent
    {
        float CurrentHealth { get; set; }
        float MaxHealth { get; }

        event Action<float>? CurrentHealthChanged;
        event Action? EntityDead;
        event Action<float>? MaxHealthChanged;

        float CurrentHealthPercent();
        void Heal(float amount);
        void HealUpToMax();
        void OnParameterChanges(object? sender, IModifiersChangedEventArgs args);
        void TakeDamage(float damage);
    }
}

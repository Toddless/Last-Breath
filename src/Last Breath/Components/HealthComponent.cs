namespace LastBreath.Components
{
    using Godot;
    using System;
    using Core.Enums;
    using LastBreath.Script.Helpers;
    using Core.Interfaces.Components;

    public class HealthComponent : IHealthComponent
    {
        private const float BaseHealth = 100;
        private float _currentHealth;
        private float _maxHealth;

        public event Action<float>? CurrentHealthChanged, MaxHealthChanged;
        public event Action? EntityDead;

        public float CurrentHealth
        {
            get => MathF.Max(0, _currentHealth);
            set
            {
                if (ObservableProperty.SetProperty(ref _currentHealth, value)) CurrentHealthChanged?.Invoke(CurrentHealth);
                if (_currentHealth <= 0) EntityDead?.Invoke();
            }
        }

        // I need to publish PlayerHealthChangeEvent from here.
        // then i need to create different handlers.
        // E.g one for all ui changes
        // second??

        public float MaxHealth
        {
            get => Mathf.RoundToInt(_maxHealth);
            private set => _maxHealth = value;
        }


        public HealthComponent()
        {
            CurrentHealth = MaxHealth;
        }

        // TODO: action or signal for hp == 0
        public void TakeDamage(float damage) => CurrentHealth = Math.Max(0, CurrentHealth - damage);

        public void Heal(float amount) => CurrentHealth = Math.Min(MaxHealth, CurrentHealth + amount);

        public void HealUpToMax() => CurrentHealth = MaxHealth;

        public void OnParameterChanges(object? sender, IModifiersChangedEventArgs args)
        {
            if (args.Parameter != Parameter.Health)
                return;
            var newMaxHealth = Calculations.CalculateFloatValue(BaseHealth, args.Modifiers);
            if (MathF.Abs(newMaxHealth - MaxHealth) > float.Epsilon)
            {
                MaxHealth = newMaxHealth;
                MaxHealthChanged?.Invoke(_maxHealth);
                if (CurrentHealth > MaxHealth)
                    CurrentHealth = MaxHealth;
            }
        }

        public float CurrentHealthPercent() => CurrentHealth / MaxHealth;
    }
}

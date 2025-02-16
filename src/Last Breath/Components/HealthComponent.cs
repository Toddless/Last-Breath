namespace Playground
{
    using System;
    using Godot;
    using Playground.Components;
    using Playground.Script.Enums;

    public class HealthComponent : ComponentBase, IHealthComponent
    {
        private readonly float _baseHealth = 100;
        private float _increaseHealth = 1;
        private float _additionalHealth;
        private float _currentHealth;
        private float _maxHealth;

        public event Action<float>? CurrentHealthChanged, MaxHealthChanged;

        public float CurrentHealth
        {
            get
            {
                if (_currentHealth <= 0)
                {
                    _currentHealth = 0;
                    return _currentHealth;
                }
                return Mathf.RoundToInt(_currentHealth);
            }
            set
            {
                if (SetProperty(ref _currentHealth, Math.Min(value, _maxHealth)))
                    CurrentHealthChanged?.Invoke(value);
            }
        }

        [Changeable]
        public float IncreaseHealth
        {
            get => _increaseHealth;
            set
            {
                if (SetProperty(ref _increaseHealth, value))
                {
                    UpdateProperties();
                }
            }
        }

        [Changeable]
        public float AdditionalHealth
        {
            get => _additionalHealth;
            set
            {
                if (SetProperty(ref _additionalHealth, value))
                {
                    UpdateProperties();
                }
            }
        }

        public float MaxHealth
        {
            get => Mathf.RoundToInt(_maxHealth);
            private set
            {
                if (SetProperty(ref _maxHealth, value))
                    MaxHealthChanged?.Invoke(value);
            }
        }

        public HealthComponent(Func<float, float, float, Parameter, float> calculateValue) : base(calculateValue)
        {
            UpdateProperties();
            RefreshHealth();
        }

        public void RefreshHealth() => CurrentHealth = MaxHealth;

        public void TakeDamage(float damage) => CurrentHealth = Math.Max(0, CurrentHealth - damage);

        public void Heal(float amount) => CurrentHealth = Math.Min(MaxHealth, CurrentHealth + amount);

        public override void UpdateProperties()
        {
            var oldMaxHealth = MaxHealth;
            MaxHealth = CalculateValues.Invoke(_baseHealth, AdditionalHealth, IncreaseHealth, Parameter.Health);
            CurrentHealth = (CurrentHealth / oldMaxHealth) * MaxHealth;
            if (CurrentHealth > MaxHealth)
                CurrentHealth = MaxHealth;
        }
    }
}

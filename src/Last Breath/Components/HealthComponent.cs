namespace Playground
{
    using System;
    using System.Collections.ObjectModel;
    using Godot;
    using Playground.Components;
    using Playground.Script.Enums;
    using Playground.Script.Passives;

    public class HealthComponent : ComponentBase, IHealthComponent
    {
        private readonly float _baseHealth = 100;
        private float _increaseHealth = 1;
        private float _additionalHealth;
        private float _currentHealth;
        private float _maxHealth;

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
                if (value > _maxHealth)
                {
                    _currentHealth = _maxHealth;
                }
                else
                {
                    _currentHealth = value;
                }
            }
        }

        public float IncreaseHealth
        {
            get => _increaseHealth;
            set
            {
                if (SetProperty(ref _increaseHealth, value))
                    UpdateValues();
            }
        }

        public float AdditionalHealth
        {
            get => _additionalHealth;
            set
            {
                if (SetProperty(ref _additionalHealth, value))
                    UpdateValues();
            }
        }

        public float MaxHealth
        {
            get => Mathf.RoundToInt(_maxHealth);
            private set => _maxHealth = value;
        }

        public HealthComponent(ObservableCollection<IEffect>? appliedEffects = default) : base(appliedEffects)
        {
            _maxHealth = (_baseHealth + AdditionalHealth) * IncreaseHealth;
            RefreshHealth();
        }

        public void RefreshHealth() => CurrentHealth = MaxHealth;

        public void TakeDamage(float damage) => CurrentHealth = Math.Max(0, CurrentHealth - damage);

        public void Heal(float amount) => CurrentHealth = Math.Min(MaxHealth, CurrentHealth + amount);

        protected override void UpdateValues()
        {
            var oldMaxHealth = MaxHealth;
            MaxHealth = CalculateValues(_baseHealth, AdditionalHealth, IncreaseHealth, Stats.Health);
            CurrentHealth = (CurrentHealth / oldMaxHealth) * MaxHealth;
            if (CurrentHealth > MaxHealth)
                CurrentHealth = MaxHealth;
        }
    }
}

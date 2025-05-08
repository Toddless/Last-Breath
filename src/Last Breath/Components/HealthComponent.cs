namespace Playground.Components
{
    using System;
    using System.Collections.Generic;
    using Godot;
    using Playground.Script.Abilities.Modifiers;
    using Playground.Script.Enums;
    using Playground.Script.Helpers;

    public class HealthComponent
    {
        private const float BaseHealth = 100;
        private float _currentHealth;
        private float _maxHealth;

        public event Action<float>? CurrentHealthChanged, MaxHealthChanged;

        public float CurrentHealth
        {
            get => MathF.Max(0, _currentHealth);
            set
            {
                if (ObservableProperty.SetProperty(ref _currentHealth, value))
                {
                    CurrentHealthChanged?.Invoke(_currentHealth);
                }
            }
        }

        public float MaxHealth => Mathf.RoundToInt(_maxHealth);

        public HealthComponent()
        {
            _currentHealth = _maxHealth;
        }

        // TODO: action or signal for hp == 0
        public void TakeDamage(float damage) => CurrentHealth = Math.Max(0, CurrentHealth - damage);

        public void Heal(float amount) => CurrentHealth = Math.Min(MaxHealth, CurrentHealth + amount);

        public void HealUpToMax() => CurrentHealth = MaxHealth;

        public void OnParameterChanges(Parameter parameter, List<IModifier> modifiers)
        {
            if (parameter != Parameter.MaxHealth)
                return;
            var newMaxHealth = Calculations.CalculateFloatValue(BaseHealth, modifiers);
            if (MathF.Abs(newMaxHealth - _maxHealth) > float.Epsilon)
            {
                _maxHealth = newMaxHealth;
                MaxHealthChanged?.Invoke(_maxHealth);
                if (CurrentHealth > MaxHealth)
                    CurrentHealth = MaxHealth;
            }
        }
    }
}

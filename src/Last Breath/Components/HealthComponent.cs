namespace Playground.Components
{
    using System;
    using Godot;
    using Playground.Script.Enums;
    using Playground.Script.Helpers;

    public class HealthComponent
    {
        private const float BaseHealth = 100;
        private float _currentHealth;
        private float _maxHealth;
        private readonly ModifierManager _modifierManager;

        public event Action<float>? CurrentHealthChanged, MaxHealthChanged;

        public float CurrentHealth
        {
            get => MathF.Max(0, _currentHealth);
            private set
            {
                GD.Print($"Old current health: {_currentHealth}");
                if (ObservableProperty.SetProperty(ref _currentHealth, value))
                {
                    CurrentHealthChanged?.Invoke(_currentHealth);
                    GD.Print($"New current health: {value}");
                }
            }
        }

        public float MaxHealth => Mathf.RoundToInt(_maxHealth);

        public HealthComponent(ModifierManager modifierManager)
        {
            _modifierManager = modifierManager;
            _modifierManager.ParameterModifiersChanged += OnParameterModifiersChanges;
            UpdateMaxHealth();
            _currentHealth = _maxHealth;

        }

        // TODO: action or signal for hp == 0
        public void TakeDamage(float damage) => CurrentHealth = Math.Max(0, CurrentHealth - damage);

        public void Heal(float amount) => CurrentHealth = Math.Min(MaxHealth, CurrentHealth + amount);

        public void HealUpToMax() => CurrentHealth = MaxHealth;

        private void OnParameterModifiersChanges(Parameter parameter)
        {
            if (parameter != Parameter.MaxHealth)
                return;
            UpdateMaxHealth();
        }

        private void UpdateMaxHealth()
        {
            GD.Print($"Old max health: {_maxHealth}");
            var newMaxHealth = Calculations.CalculateFloatValue(BaseHealth, _modifierManager.GetCombinedModifiers(Parameter.MaxHealth));
            if (MathF.Abs(newMaxHealth - _maxHealth) > float.Epsilon)
            {
                _maxHealth = newMaxHealth;
                GD.Print($"Max health set to: {newMaxHealth}");
                MaxHealthChanged?.Invoke(_maxHealth);
                if (CurrentHealth > MaxHealth)
                    CurrentHealth = MaxHealth;
            }
        }
    }
}

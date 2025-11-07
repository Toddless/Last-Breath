namespace Battle.Components
{
    using Godot;
    using System;
    using Source;
    using Core.Enums;
    using Source.Module.StatModules;
    using Core.Interfaces.Components;
    using System.Collections.Generic;
    using Core.Interfaces.Battle.Module;
    using Core.Interfaces.Battle.Decorator;

    public class HealthComponent : Component, IHealthComponent
    {
        private float _currentHealth;

        public event Action<float>? CurrentHealthChanged;
        public event Action? EntityDead;

        public float MaxHealth => this[Parameter.Health].GetValue();
        public float HealthRecovery => this[Parameter.HealthRecovery].GetValue();

        public float CurrentHealth
        {
            get => Mathf.Max(0, _currentHealth);
            private set
            {
                float clamped = Mathf.Max(0, Mathf.Min(value, MaxHealth));
                if (Mathf.Abs(clamped - _currentHealth) < float.Epsilon) return;
                _currentHealth = clamped;
                CurrentHealthChanged?.Invoke(_currentHealth);
                if (_currentHealth <= 0f)
                    EntityDead?.Invoke();
            }
        }

        public HealthComponent()
        {
            Parameters = new() { [Parameter.Health] = (100f, 100f), [Parameter.HealthRecovery] = (1f, 1f) };
            ModuleManager = new ModuleManager<Parameter, IParameterModule, StatModuleDecorator>(
                new Dictionary<Parameter, IParameterModule>
                {
                    [Parameter.Health] = new HealthModule(() => Parameters[Parameter.Health].Current),
                    [Parameter.HealthRecovery] =
                        new HealthRecoveryModule(() => Parameters[Parameter.HealthRecovery].Current)
                });

            SetModule();

            CurrentHealth = MaxHealth;
        }

        public override void OnParameterChanges(object? sender, IModifiersChangedEventArgs args)
        {
            base.OnParameterChanges(sender, args);
            if (args.Parameter != Parameter.Health) return;

            float newValue = Stats[args.Parameter].GetValue();
            if (CurrentHealth > newValue) CurrentHealth = newValue;
        }

        public void TakeDamage(float damage) => CurrentHealth -= damage;
        public void Heal(float amount) => CurrentHealth += amount;
        public void HealUpToMax() => CurrentHealth = MaxHealth;
        public float CurrentHealthPercent() => MaxHealth <= 0f ? 0f : CurrentHealth / MaxHealth;

        private void SetModule()
        {
            foreach (var param in Parameters.Keys)
                Stats[param] = ModuleManager.GetModule(param);
        }
    }
}

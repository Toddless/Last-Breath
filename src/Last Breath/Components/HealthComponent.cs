namespace Playground
{
    using Godot;
    using Playground.Components;

    public class HealthComponent : ComponentBase, IHealthComponent
    {
        private readonly float _baseHealth = 100;
        private float _totalPercentHealthIncreases = 1;
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

        public float TotalPercentHealthIncreases
        {
            get => _totalPercentHealthIncreases;
            set
            {
                SetProperty(ref _totalPercentHealthIncreases, value);
            }
        }

        public float AdditionalHealth
        {
            get => _additionalHealth;
            set
            {
                SetProperty(ref _additionalHealth, value);
            }
        }

        public float MaxHealth
        {
            get => Mathf.RoundToInt(_maxHealth);
            private set => _maxHealth = value;
        }

        protected float BaseHealth
        {
            get => _baseHealth;
        }

        public HealthComponent()
        {
            _maxHealth = (BaseHealth + AdditionalHealth) * TotalPercentHealthIncreases;
            RefreshHealth();
        }

        public void RefreshHealth()
        {
            CurrentHealth = MaxHealth;
        }

        public void TakeDamage(float damage) => CurrentHealth -= damage;
        public void Heal(float amount) => CurrentHealth += amount;
        public void ReducePercentageHealth(float percentage) => MaxHealth *= percentage;
    }
}

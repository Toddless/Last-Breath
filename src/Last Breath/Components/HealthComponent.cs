namespace Playground
{
    using Godot;
    using Playground.Components;

    [GlobalClass]
    public partial class HealthComponent : ComponentBase, IHealthComponent
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
                if (SetProperty(ref _currentHealth, value))
                {
                    if (_currentHealth > _maxHealth)
                    {
                        _currentHealth = _maxHealth;
                    }
                }
            }
        }

        /// <summary>
        /// All percent increases for health (strength bonuses, passives, etc.)
        /// </summary>
        public float TotalPercentHealthIncreases
        {
            get => _totalPercentHealthIncreases;
            set
            {
                SetProperty(ref _totalPercentHealthIncreases, value);
            }
        }


        /// <summary>
        /// Flat bonus health (passives, items, etc.)
        /// </summary>
        public float AdditionalHealth
        {
            get => _additionalHealth;
            set
            {
                SetProperty(ref _additionalHealth, value);
            }
        }

        /// <summary>
        /// Sum of all bonuses ((Base health + additional health) * total percent)
        /// </summary>
        public float MaxHealth
        {
            get => Mathf.RoundToInt(_maxHealth);
            set
            {
               if( SetProperty(ref _maxHealth, value))
                {
                    if(_maxHealth < _currentHealth)
                    {
                        _currentHealth = _maxHealth;
                    }
                }
            }
        }

        protected float BaseHealth
        {
            get => _baseHealth;
        }

        public override void _Ready()
        {
            RefreshMaxHealth();
            RefreshHealth();
        }

        public void RefreshHealth()
        {
            CurrentHealth = MaxHealth;
        }

        private void RefreshMaxHealth()
        {
            MaxHealth = (BaseHealth + AdditionalHealth) * TotalPercentHealthIncreases;
        }

        public void TakeDamage(float damage) => CurrentHealth -= damage;
        public void Heal(float amount) =>  CurrentHealth += amount;
        public void ReducePercentageHealth(float percentage) => MaxHealth *= percentage;
    }
}

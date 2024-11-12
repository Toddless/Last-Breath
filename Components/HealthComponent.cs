namespace Playground
{
    using Godot;

    [GlobalClass]
    public partial class HealthComponent : Node
    {
        #region Export fields
        [Export]
        private float _currentHealth;
        [Export]
        private float _maxHealth = 100;
        [Export]
        private float _defence = 30;

        #endregion

        #region Signals
        [Signal]
        public delegate void OnCharacterDiedEventHandler();
        #endregion

        #region Properties
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
                if (_currentHealth > _maxHealth)
                {
                    _currentHealth = _maxHealth;
                }
            }
        }

        public float MaxHealth
        {
            get => _maxHealth;
            set => _maxHealth = Mathf.RoundToInt(value);
        }

        public float Defence
        {
            get => _defence;
            set => _defence = Mathf.RoundToInt(value);
        }
        #endregion

        public override void _Ready()
        {
            _currentHealth = _maxHealth;
        }

        public void TakeDamage(float damage)
        {
            _currentHealth -= damage;
        }

        public void Heal(float amount)
        {
            _currentHealth += amount;
            if (_currentHealth > _maxHealth)
                _currentHealth = _maxHealth;
        }

        public void IncreasedMaximumHealth(float amount)
        {
            _maxHealth += amount;
        }

        public void ReduceMaximumHealth(float amount)
        {
            _maxHealth -= amount;
        }

        public void RefreshHealth()
        {
            _currentHealth = MaxHealth;
        }
    }
}

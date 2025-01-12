namespace Playground
{
    using Godot;
    using Playground.Components;

    [GlobalClass]
    public partial class AttackComponent : Node, IGameComponent
    {
        #region private fields
        private readonly RandomNumberGenerator _rng = new();
        private float _additionalAdditionlaAttackChance;
        private float _currentAdditionalAttackChance;
        private float _baseAdditionalAttackChance;

        private float _additionalCriticalStrikeChance;
        private float _currentCriticalStrikeChance;
        private float _baseCriticalStrikeChance;

        private float _additionalCriticalStrikeDamage;
        private float _currentCriticalStrikeDamage;
        private float _baseCriticalStrikeDamage;

        private float _leechedHealth;

        private float _additionalMinDamage;
        private float _currentMinDamage;
        private float _baseMinDamage;

        private float _additionalMaxDamage;
        private float _currentMaxDamage;
        private float _baseMaxDamage;

        private float _leech;
        #endregion

        #region Properties

        public float BaseMinDamage
        {
            get => _baseMinDamage;
            set => _baseMinDamage = value;
        }

        public float CurrentMinDamage
        {
            get => Mathf.RoundToInt(_currentMinDamage);
            set => _currentMinDamage = value;
        }

        public float BaseMaxDamage
        {
            get => _baseMaxDamage;
            set => _baseMaxDamage = value;
        }

        public float CurrentMaxDamage
        {
            get => Mathf.RoundToInt(_currentMaxDamage);
            set => _currentMaxDamage = value;
        }

        public float BaseCriticalStrikeChance
        {
            get => _baseCriticalStrikeChance;
            set => _baseCriticalStrikeChance = value;
        }

        public float CurrentCriticalStrikeChance
        {
            get => _currentCriticalStrikeChance;
            set => _currentCriticalStrikeChance = value;
        }

        public float BaseCriticalStrikeDamage
        {
            get => _baseCriticalStrikeDamage;
            set => _baseCriticalStrikeDamage = value;
        }

        public float CurrentCriticalStrikeDamage
        {
            get => _currentCriticalStrikeDamage;
            set => _currentCriticalStrikeDamage = value;
        }

        public float Leech
        {
            get => _leech;
            set
            {
                if (value > 1f || value < 0f)
                {
                    _leech = 0;
                }
                else
                {
                    _leech = value;
                }
            }
        }

        public float LeechedHealth
        {
            get => _leechedHealth;
            set => _leechedHealth = value;
        }

        public float BaseAdditionalAttackChance
        {
            get => _baseAdditionalAttackChance;
            set => _baseAdditionalAttackChance = value;
        }

        public float CurrentAdditionalAttackChance
        {
            get => _currentAdditionalAttackChance;
            set => _currentAdditionalAttackChance = value;
        }
        #endregion

        public override void _Ready()
        {
            _baseAdditionalAttackChance = 0.05f;
            _baseCriticalStrikeChance = 0.05f;
            _baseCriticalStrikeDamage = 1.5f;
            _baseMaxDamage = 100f;
            _baseMinDamage = 25f;

            _currentMinDamage = _additionalMinDamage + _baseMinDamage;
            _currentMaxDamage = _additionalMaxDamage + _baseMaxDamage;
            _currentCriticalStrikeChance = _additionalCriticalStrikeChance + _baseCriticalStrikeChance;
            _currentCriticalStrikeDamage = _additionalCriticalStrikeDamage + _baseCriticalStrikeDamage;
            _currentAdditionalAttackChance = _additionalAdditionlaAttackChance + _baseAdditionalAttackChance;
        }

        public (float damage, bool crit) CalculateDamage()
        {
            float damage = _rng.RandfRange(_currentMinDamage, _currentMaxDamage);
            bool criticalStrike = _rng.RandfRange(0, 1) <= _currentCriticalStrikeChance;
            if (criticalStrike)
            {
                var finalDamage = damage * _currentCriticalStrikeDamage;
                _leechedHealth = _leech * finalDamage;
                return (finalDamage, true);
            }
            _leechedHealth = _leech * damage;
            return (damage, false);
        }
    }
}

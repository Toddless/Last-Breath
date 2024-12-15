namespace Playground
{
    using Godot;

    [GlobalClass]
    public partial class AttackComponent : Node
    {
        #region private fields
        private readonly RandomNumberGenerator _rng = new();
        private float _criticalStrikeChance;
        private float _criticalStrikeDamage;
        private float _baseMinDamage;
        private float _baseMaxDamage;
        private float _finalDamage;
        private float _leech = 0f;
        private float _leechedHealth = 0;
        #endregion

        #region Properties

        public float BaseMinDamage
        {
            get => _baseMinDamage;
            set => _baseMinDamage = Mathf.RoundToInt(value);
        }

        public float BaseMaxDamage
        {
            get => _baseMaxDamage;
            set => _baseMaxDamage = Mathf.RoundToInt(value);
        }

        public float CriticalStrikeChance
        {
            get => _criticalStrikeChance;
            set => _criticalStrikeChance = value;
        }

        public float CriticalStrikeDamage
        {
            get => _criticalStrikeDamage;
            set => _criticalStrikeDamage = value;
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

        #endregion

        public override void _Ready()
        {
            _criticalStrikeDamage = 1.5f;
            _criticalStrikeChance = 0.05f;
            _baseMinDamage = 25f;
            _baseMaxDamage = 100f;
        }

        public (float, bool) CalculateDamage()
        {
            float damage = _rng.RandfRange(_baseMinDamage, _baseMaxDamage);
            bool criticalStrike = _rng.RandfRange(0, 1) <= _criticalStrikeChance;
            if (criticalStrike)
            {
                var finalDamage = damage * _criticalStrikeDamage;
                _leechedHealth = _leech * finalDamage;
                return (finalDamage, true);
            }
            _leechedHealth = _leech * damage;
            return (damage, false);
        }
    }
}

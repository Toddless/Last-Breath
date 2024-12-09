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

        #endregion

        #region Signal
        [Signal]
        public delegate void OnPlayerCriticalHitEventHandler();
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
                return (damage *= _criticalStrikeDamage, true);
            }

            return (damage, false);
        }
    }
}

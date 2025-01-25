namespace Playground
{
    using Godot;
    using Playground.Components;
    using Playground.Script;
    using Playground.Script.Enums;

    [Inject]
    public class AttackComponent : ComponentBase, IAttackComponent
    {
        #region private fields
        private RandomNumberGenerator? _rnd;
        private readonly float _baseAdditionalStrikeChance = 0.05f;
        private readonly float _baseCriticalStrikeChance = 0.05f;
        private readonly float _baseCriticalStrikeDamage = 1.5f;
        private readonly float _baseMinStrikeDamage = 40;
        private readonly float _baseMaxStrikeDamage = 100;

        private float _additionalAdditionalStrikeChance;
        private float _increaseAdditionalStrikeChance = 1;
        private float _currentAdditionalStrikeChance;

        private float _additionalCriticalStrikeChance;
        private float _increaseCriticalStrikeChance = 1;
        private float _currentCriticalStrikeChance;

        private float _additionalCriticalStrikeDamage;
        private float _currentCriticalStrikeDamage;

        private float _additionalMinStrikeDamage;
        private float _currentMinStrikeDamage;

        private float _additionalMaxStrikeDamage;
        private float _currentMaxStrikeDamage;

        private float _increaseDamage = 1;

        private float _leech;
        #endregion

        #region Properties

        [Inject]
        protected RandomNumberGenerator? Rnd
        {
            get => _rnd;
            set => _rnd = value;
        }
        #region Additional Values

        public float AdditionalMinStrikeDamage
        {
            get => _additionalMinStrikeDamage;
            set
            {
                if (SetProperty(ref _additionalMinStrikeDamage, value))
                    UpdateValues();
            }
        }

        public float AdditionalMaxStrikeDamage
        {
            get => _additionalMaxStrikeDamage;
            set
            {
                if (SetProperty(ref _additionalMaxStrikeDamage, value))
                    UpdateValues();
            }
        }

        public float AdditionalCriticalStrikeDamage
        {
            get => _additionalCriticalStrikeDamage;
            set
            {
                if (SetProperty(ref _additionalCriticalStrikeDamage, value))
                    UpdateValues();
            }
        }

        public float AdditionalCriticalStrikeChance
        {
            get => _additionalCriticalStrikeChance;
            set
            {
                if (SetProperty(ref _additionalCriticalStrikeChance, value))
                    UpdateValues();
            }
        }

        public float AdditionalAdditionalStrikeChance
        {
            get => _additionalAdditionalStrikeChance;
            set
            {
                if (SetProperty(ref _additionalAdditionalStrikeChance, value))
                    UpdateValues();
            }
        }

        public float IncreaseDamage
        {
            get => _increaseDamage;
            set
            {
                if (SetProperty(ref _increaseDamage, value))
                    UpdateValues();
            }
        }

        public float IncreaseCriticalStrikeChance
        {
            get => _increaseCriticalStrikeChance;
            set
            {
                if (SetProperty(ref _increaseCriticalStrikeChance, value))
                    UpdateValues();
            }
        }

        public float IncreaseAdditionalStrikeChance
        {
            get => _increaseAdditionalStrikeChance;
            set
            {
                if (SetProperty(ref _increaseAdditionalStrikeChance, value))
                    UpdateValues();
            }
        }

        #endregion

        #region Current Values
        public float CurrentMinStrikeDamage
        {
            get => Mathf.RoundToInt(_currentMinStrikeDamage);
            private set => _currentMinStrikeDamage = value;
        }

        public float CurrentMaxStrikeDamage
        {
            get => Mathf.RoundToInt(_currentMaxStrikeDamage);
            private set => _currentMaxStrikeDamage = value;
        }

        public float CurrentCriticalStrikeChance
        {
            get => _currentCriticalStrikeChance;
            private set => _currentCriticalStrikeChance = value;
        }

        public float CurrentCriticalStrikeDamage
        {
            get => _currentCriticalStrikeDamage;
            private set => _currentCriticalStrikeDamage = value;
        }

        public float CurrentAdditionalStrikeChance
        {
            get => _currentAdditionalStrikeChance;
            private set => _currentAdditionalStrikeChance = value;
        }

        #endregion

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

        #endregion

        public AttackComponent()
        {
            CurrentMinStrikeDamage = (AdditionalMinStrikeDamage + _baseMinStrikeDamage) * IncreaseDamage;
            CurrentMaxStrikeDamage = (AdditionalMaxStrikeDamage + _baseMaxStrikeDamage) * IncreaseDamage;
            CurrentCriticalStrikeChance = (AdditionalCriticalStrikeChance + _baseCriticalStrikeChance) * IncreaseCriticalStrikeChance;
            CurrentAdditionalStrikeChance = (AdditionalAdditionalStrikeChance + _baseAdditionalStrikeChance) * IncreaseAdditionalStrikeChance;
            CurrentCriticalStrikeDamage = AdditionalCriticalStrikeDamage + _baseCriticalStrikeDamage;
            DiContainer.InjectDependencies(this);
        }

        protected override void UpdateValues()
        {
            CurrentMaxStrikeDamage = CalculateValues(_baseMaxStrikeDamage, AdditionalMaxStrikeDamage, IncreaseDamage, Stats.StrikeDamage);
            CurrentMinStrikeDamage = CalculateValues(_baseMinStrikeDamage, AdditionalMinStrikeDamage, IncreaseDamage, Stats.StrikeDamage);
            CurrentCriticalStrikeChance = CalculateValues(_baseCriticalStrikeChance, AdditionalCriticalStrikeChance, IncreaseCriticalStrikeChance, Stats.CriticalStrikeChance);
            CurrentCriticalStrikeDamage = CalculateValues(_baseCriticalStrikeDamage, AdditionalCriticalStrikeDamage, 1f, Stats.CriticalStrikeDamage);
            CurrentAdditionalStrikeChance = CalculateValues(_baseAdditionalStrikeChance, AdditionalAdditionalStrikeChance, IncreaseAdditionalStrikeChance, Stats.AdditionalStrikeChance);
        }

        public (float damage, bool crit, float leechedDamage) CalculateDamage()
        {
            float damage = Rnd!.RandfRange(CurrentMinStrikeDamage, CurrentMaxStrikeDamage);
            bool criticalStrike = Rnd.RandfRange(0, 1) <= CurrentCriticalStrikeChance;
            if (criticalStrike)
            {
                var finalDamage = damage * CurrentCriticalStrikeDamage;
                return (finalDamage, true, Leech * finalDamage);
            }
            return (damage, false, Leech * damage);
        }

        protected bool IsChanceSuccessful(float chance)
        {
            return Rnd!.Randf() <= chance;
        }
    }
}

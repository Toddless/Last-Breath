namespace Playground
{
    using System;
    using Godot;
    using Playground.Components;
    using Playground.Script;
    using Playground.Script.Enums;

    [Inject]
    public class AttackComponent : ComponentBase, IAttackComponent
    {
        #region private fields

        #region Base values
        private readonly float _baseExtraHitChance = 0.05f;
        private readonly float _baseCriticalChance = 0.05f;
        private readonly float _baseCriticalDamage = 1.5f;
        private readonly float _baseMinDamage = 40;
        private readonly float _baseMaxDamage = 100;
        #endregion

        #region Increases
        private float _increaseCriticalChance = 1;
        private float _increaseExtraHitChance = 1;
        private float _increaseDamage = 1;
        #endregion

        #region Additionals
        private float _additionalExtraHitChance;
        private float _additionalCriticalChance;
        private float _additionalCriticalDamage;
        private float _additionalMinDamage;
        private float _additionalMaxDamage;
        #endregion

        #region Currents
        private float _currentExtraHitChance;
        private float _currentCriticalDamage;
        private float _currentCriticalChance;
        private float _currentMinDamage;
        private float _currentMaxDamage;
        #endregion

        private RandomNumberGenerator? _rnd;
        private float _leech;
        #endregion

        #region Properties

        [Inject]
        protected RandomNumberGenerator? Rnd
        {
            get => _rnd;
            set => _rnd = value;
        }

        public event Action<float>? CurrentCriticalChanceChanged, CurrentCriticalDamageChanged, CurrentExtraHitChanged;
        public event Action<float, float> CurrentDamageChanged;
        #region Additional Values

        public float AdditionalMinDamage
        {
            get => _additionalMinDamage;
            set
            {
                value = ClampValue(value, 0);
                if (SetProperty(ref _additionalMinDamage, value))
                    CurrentMinDamage = CalculateValues.Invoke(_baseMinDamage, AdditionalMinDamage, IncreaseDamage, Parameter.StrikeDamage);
            }
        }

        public float AdditionalMaxDamage
        {
            get => _additionalMaxDamage;
            set
            {
                value = ClampValue(value, 0);
                if (SetProperty(ref _additionalMaxDamage, value))
                    CurrentMaxDamage = CalculateValues.Invoke(_baseMaxDamage, AdditionalMaxDamage, IncreaseDamage, Parameter.StrikeDamage);
            }
        }

        public float AdditionalCriticalDamage
        {
            get => _additionalCriticalDamage;
            set
            {
                value = ClampValue(value, 0);
                if (SetProperty(ref _additionalCriticalDamage, value))
                    CurrentCriticalDamage = CalculateValues.Invoke(_baseCriticalDamage, AdditionalCriticalDamage, 1f, Parameter.CriticalStrikeDamage);
            }
        }

        public float AdditionalCriticalChance
        {
            get => _additionalCriticalChance;
            set
            {
                value = ClampValue(value, 0);
                if (SetProperty(ref _additionalCriticalChance, value))
                    CurrentCriticalChance = CalculateValues.Invoke(_baseCriticalChance, AdditionalCriticalChance, IncreaseCriticalChance, Parameter.CriticalStrikeChance);
            }
        }

        public float AdditionalExtraHitChance
        {
            get => _additionalExtraHitChance;
            set
            {
                value = ClampValue(value, 0);
                if (SetProperty(ref _additionalExtraHitChance, value))
                    CurrentExtraHitChance = CalculateValues.Invoke(_baseExtraHitChance, AdditionalExtraHitChance, IncreaseExtraHitChance, Parameter.AdditionalStrikeChance);
            }
        }

        public float IncreaseCriticalChance
        {
            get => _increaseCriticalChance;
            set
            {
                value = ClampValue(value, 0);
                if (SetProperty(ref _increaseCriticalChance, value))
                    CurrentCriticalChance = CalculateValues.Invoke(_baseCriticalChance, AdditionalCriticalChance, IncreaseCriticalChance, Parameter.CriticalStrikeChance);
            }
        }

        public float IncreaseExtraHitChance
        {
            get => _increaseExtraHitChance;
            set
            {
                value = ClampValue(value, 0);
                if (SetProperty(ref _increaseExtraHitChance, value))
                    CurrentExtraHitChance = CalculateValues.Invoke(_baseExtraHitChance, AdditionalExtraHitChance, IncreaseExtraHitChance, Parameter.AdditionalStrikeChance);
            }

        }

        public float IncreaseDamage
        {
            get => _increaseDamage;
            set
            {
                value = ClampValue(value, 0);
                if (SetProperty(ref _increaseDamage, value))
                    UpdateIncreaseDamageValues();
            }
        }
        #endregion

        #region Current Values
        public float CurrentMinDamage
        {
            get => Mathf.RoundToInt(_currentMinDamage);
            private set
            {
                if (SetProperty(ref _currentMinDamage, value))
                    CurrentDamageChanged?.Invoke(value, CurrentMaxDamage);
            }
        }

        public float CurrentMaxDamage
        {
            get => Mathf.RoundToInt(_currentMaxDamage);
            private set
            {
                if (SetProperty(ref _currentMaxDamage, value))
                    CurrentDamageChanged?.Invoke(CurrentMinDamage, value);
            }
        }

        public float CurrentCriticalChance
        {
            get => _currentCriticalChance;
            private set
            {
                if (SetProperty(ref _currentCriticalChance, value))
                    CurrentCriticalChanceChanged?.Invoke(value);
            }
        }

        public float CurrentCriticalDamage
        {
            get => _currentCriticalDamage;
            private set
            {
                if (SetProperty(ref _currentCriticalDamage, value))
                    CurrentCriticalDamageChanged?.Invoke(value);
            }
        }

        public float CurrentExtraHitChance
        {
            get => _currentExtraHitChance;
            private set
            {
                if (SetProperty(ref _currentExtraHitChance, value))
                    CurrentExtraHitChanged?.Invoke(value);
            }
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

        public AttackComponent(Func<float, float, float, Parameter, float> calculateValue) : base(calculateValue)
        {
            UpdateProperties();
            // since a lot of code was changed, i need better solution for this
            DiContainer.InjectDependencies(this);
        }

        public override void UpdateProperties()
        {
            CurrentMaxDamage = CalculateValues.Invoke(_baseMaxDamage, AdditionalMaxDamage, IncreaseDamage, Parameter.StrikeDamage);
            CurrentMinDamage = CalculateValues.Invoke(_baseMinDamage, AdditionalMinDamage, IncreaseDamage, Parameter.StrikeDamage);
            CurrentCriticalChance = CalculateValues.Invoke(_baseCriticalChance, AdditionalCriticalChance, IncreaseCriticalChance, Parameter.CriticalStrikeChance);
            CurrentCriticalDamage = CalculateValues.Invoke(_baseCriticalDamage, AdditionalCriticalDamage, 1f, Parameter.CriticalStrikeDamage);
            CurrentExtraHitChance = CalculateValues.Invoke(_baseExtraHitChance, AdditionalExtraHitChance, IncreaseExtraHitChance, Parameter.AdditionalStrikeChance);
        }

        private void UpdateIncreaseDamageValues()
        {
            CurrentMaxDamage = CalculateValues.Invoke(_baseMaxDamage, AdditionalMaxDamage, IncreaseDamage, Parameter.StrikeDamage);
            CurrentMinDamage = CalculateValues.Invoke(_baseMinDamage, AdditionalMinDamage, IncreaseDamage, Parameter.StrikeDamage);
        }

        // i don´t really like it, refactoring is needed
        public (float damage, bool crit, float leechedDamage) CalculateDamage()
        {
            float damage = Rnd!.RandfRange(CurrentMinDamage, CurrentMaxDamage);
            bool criticalStrike = Rnd.RandfRange(0, 1) <= CurrentCriticalChance;
            if (criticalStrike)
            {
                var finalDamage = damage * CurrentCriticalDamage;
                return (finalDamage, true, Leech * finalDamage);
            }
            return (damage, false, Leech * damage);
        }

        // looks useful
        public bool IsChanceSuccessful(float chance)
        {
            return Rnd!.Randf() <= chance;
        }

        private float ClampValue(float value, float minValue) => value < minValue ? minValue : value;
    }
}

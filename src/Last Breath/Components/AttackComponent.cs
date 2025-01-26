namespace Playground
{
    using System.Collections.ObjectModel;
    using Godot;
    using Playground.Components;
    using Playground.Script;
    using Playground.Script.Enums;
    using Playground.Script.Passives;

    [Inject]
    public class AttackComponent : ComponentBase, IAttackComponent
    {
        #region private fields

        #region Base values
        private readonly float _baseAdditionalStrikeChance = 0.05f;
        private readonly float _baseCriticalStrikeChance = 0.05f;
        private readonly float _baseCriticalStrikeDamage = 1.5f;
        private readonly float _baseMinStrikeDamage = 40;
        private readonly float _baseMaxStrikeDamage = 100;
        #endregion

        #region Increases
        private float _increaseCriticalStrikeChance = 1;
        private float _increaseAdditionalStrikeChance = 1;
        #endregion

        #region Additionals
        private float _additionalAdditionalStrikeChance;
        private float _additionalCriticalStrikeChance;
        private float _additionalCriticalStrikeDamage;
        private float _additionalMinStrikeDamage;
        private float _additionalMaxStrikeDamage;
        #endregion

        #region Currents
        private float _currentAdditionalStrikeChance;
        private float _currentCriticalStrikeDamage;
        private float _currentCriticalStrikeChance;
        private float _currentMinStrikeDamage;
        private float _currentMaxStrikeDamage;
        #endregion

        private RandomNumberGenerator? _rnd;
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
                    UpdateAdditionalMinDamageValue();
            }
        }

        public float AdditionalMaxStrikeDamage
        {
            get => _additionalMaxStrikeDamage;
            set
            {
                if (SetProperty(ref _additionalMaxStrikeDamage, value))
                    UpdateAdditionalMaxDamageValue();
            }
        }

        public float AdditionalCriticalStrikeDamage
        {
            get => _additionalCriticalStrikeDamage;
            set
            {
                if (SetProperty(ref _additionalCriticalStrikeDamage, value))
                    UpdateAdditionalCriticalStrikeDamageValue();
            }
        }

        public float AdditionalCriticalStrikeChance
        {
            get => _additionalCriticalStrikeChance;
            set
            {
                if (SetProperty(ref _additionalCriticalStrikeChance, value))
                    UpdateAdditionalCriticalStrikeChanceValue();
            }
        }

        public float AdditionalAdditionalStrikeChance
        {
            get => _additionalAdditionalStrikeChance;
            set
            {
                if (SetProperty(ref _additionalAdditionalStrikeChance, value))
                    UpdateAdditionalStrikeChanceValue();
            }
        }

        public float IncreaseDamage
        {
            get => _increaseDamage;
            set
            {
                if (SetProperty(ref _increaseDamage, value))
                    UpdateIncreaseDamageValues();
            }
        }

        public float IncreaseCriticalStrikeChance
        {
            get => _increaseCriticalStrikeChance;
            set
            {
                if (SetProperty(ref _increaseCriticalStrikeChance, value))
                    UpdateIncreaseCriticalStrikeChanceValue();
            }
        }

        public float IncreaseAdditionalStrikeChance
        {
            get => _increaseAdditionalStrikeChance;
            set
            {
                if (SetProperty(ref _increaseAdditionalStrikeChance, value))
                    UpdateIncreaseAdditionalStrikeChanceValue();
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
        public AttackComponent(ObservableCollection<IEffect>? appliedEffects = default) : base(appliedEffects)
        {
            UpdateValues();
            // since a lot of code was changed, i need better solution for this
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

        // Not very pretty, but I don't want to recalculate all properties via the UpdateValues method when only one thing has been changed
        // Switch statement not helping if more than one property was changed simultaneously
        // Maybe I'll find a better solution later
        #region Pain for the eyes

        private void UpdateAdditionalMinDamageValue() => CurrentMinStrikeDamage = CalculateValues(_baseMinStrikeDamage, AdditionalMinStrikeDamage, IncreaseDamage, Stats.StrikeDamage);
        private void UpdateAdditionalMaxDamageValue() => CurrentMaxStrikeDamage = CalculateValues(_baseMaxStrikeDamage, AdditionalMaxStrikeDamage, IncreaseDamage, Stats.StrikeDamage);
        private void UpdateAdditionalCriticalStrikeDamageValue() => CurrentCriticalStrikeDamage = CalculateValues(_baseCriticalStrikeDamage, AdditionalCriticalStrikeDamage, 1f, Stats.CriticalStrikeDamage);
        private void UpdateAdditionalCriticalStrikeChanceValue() => CurrentCriticalStrikeChance = CalculateValues(_baseCriticalStrikeChance, AdditionalCriticalStrikeChance, IncreaseCriticalStrikeChance, Stats.CriticalStrikeChance);
        private void UpdateIncreaseAdditionalStrikeChanceValue() => CurrentAdditionalStrikeChance = CalculateValues(_baseAdditionalStrikeChance, AdditionalAdditionalStrikeChance, IncreaseAdditionalStrikeChance, Stats.AdditionalStrikeChance);
        private void UpdateIncreaseCriticalStrikeChanceValue() => CurrentCriticalStrikeChance = CalculateValues(_baseCriticalStrikeChance, AdditionalCriticalStrikeChance, IncreaseCriticalStrikeChance, Stats.CriticalStrikeChance);
        private void UpdateAdditionalStrikeChanceValue() => CurrentAdditionalStrikeChance = CalculateValues(_baseAdditionalStrikeChance, AdditionalAdditionalStrikeChance, IncreaseAdditionalStrikeChance, Stats.AdditionalStrikeChance);
        private void UpdateIncreaseDamageValues()
        {
            CurrentMaxStrikeDamage = CalculateValues(_baseMaxStrikeDamage, AdditionalMaxStrikeDamage, IncreaseDamage, Stats.StrikeDamage);
            CurrentMinStrikeDamage = CalculateValues(_baseMinStrikeDamage, AdditionalMinStrikeDamage, IncreaseDamage, Stats.StrikeDamage);
        }
        #endregion


        // i don´t really like it, refactoring is needed
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

        // looks useful, but im not sure
        protected bool IsChanceSuccessful(float chance)
        {
            return Rnd!.Randf() <= chance;
        }
    }
}

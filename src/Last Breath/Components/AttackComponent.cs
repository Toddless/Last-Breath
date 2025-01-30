namespace Playground
{
    using System;
    using System.Collections.ObjectModel;
    using Godot;
    using Playground.Components;
    using Playground.Script;
    using Playground.Script.Effects.Interfaces;
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

        public float AdditionalMinDamage
        {
            get => _additionalMinDamage;
            set => UpdateProperty(ref _additionalMinDamage, CalculateValues(_baseMinDamage, AdditionalMinDamage, IncreaseDamage, Parameter.StrikeDamage), value => CurrentMinDamage = value);
        }

        public float AdditionalMaxDamage
        {
            get => _additionalMaxDamage;
            set => UpdateProperty(ref _additionalMaxDamage, CalculateValues(_baseMaxDamage, AdditionalMaxDamage, IncreaseDamage, Parameter.StrikeDamage), value => CurrentMaxDamage = value);
        }

        public float AdditionalCriticalDamage
        {
            get => _additionalCriticalDamage;
            set => UpdateProperty(ref _additionalCriticalDamage, CalculateValues(_baseCriticalDamage, AdditionalCriticalDamage, 1f, Parameter.CriticalStrikeDamage), value => CurrentCriticalDamage = value);
        }

        public float AdditionalCriticalChance
        {
            get => _additionalCriticalChance;
            set => UpdateProperty(ref _additionalCriticalChance, CalculateValues(_baseCriticalChance, AdditionalCriticalChance, IncreaseCriticalChance, Parameter.CriticalStrikeChance), value => CurrentCriticalChance = value);
        }

        public float AdditionalExtraHitChance
        {
            get => _additionalExtraHitChance;
            set => UpdateProperty(ref _additionalExtraHitChance, CalculateValues(_baseExtraHitChance, AdditionalExtraHitChance, IncreaseExtraHitChance, Parameter.AdditionalStrikeChance), value => CurrentExtraHitChance = value);
        }

        public float IncreaseCriticalChance
        {
            get => _increaseCriticalChance;
            set => UpdateProperty(ref _increaseCriticalChance, CalculateValues(_baseCriticalChance, AdditionalCriticalChance, IncreaseCriticalChance, Parameter.CriticalStrikeChance), value => CurrentCriticalChance = value);
        }

        public float IncreaseExtraHitChance
        {
            get => _increaseExtraHitChance;
            set => UpdateProperty(ref _increaseExtraHitChance, CalculateValues(_baseExtraHitChance, AdditionalExtraHitChance, IncreaseExtraHitChance, Parameter.AdditionalStrikeChance), value => CurrentExtraHitChance = value);

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
        #endregion

        #region Current Values
        public float CurrentMinDamage
        {
            get => Mathf.RoundToInt(_currentMinDamage);
            private set => _currentMinDamage = value;
        }

        public float CurrentMaxDamage
        {
            get => Mathf.RoundToInt(_currentMaxDamage);
            private set => _currentMaxDamage = value;
        }

        public float CurrentCriticalChance
        {
            get => _currentCriticalChance;
            private set => _currentCriticalChance = value;
        }

        public float CurrentCriticalDamage
        {
            get => _currentCriticalDamage;
            private set => _currentCriticalDamage = value;
        }

        public float CurrentExtraHitChance
        {
            get => _currentExtraHitChance;
            private set => _currentExtraHitChance = value;
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

        public AttackComponent(Func<Parameter, (float, float)> getModifiers) : base(getModifiers)
        {
            UpdateProperties();
            // since a lot of code was changed, i need better solution for this
            DiContainer.InjectDependencies(this);
        }

        public override void UpdateProperties()
        {
            CurrentMaxDamage = CalculateValues(_baseMaxDamage, AdditionalMaxDamage, IncreaseDamage, Parameter.StrikeDamage);
            CurrentMinDamage = CalculateValues(_baseMinDamage, AdditionalMinDamage, IncreaseDamage, Parameter.StrikeDamage);
            CurrentCriticalChance = CalculateValues(_baseCriticalChance, AdditionalCriticalChance, IncreaseCriticalChance, Parameter.CriticalStrikeChance);
            CurrentCriticalDamage = CalculateValues(_baseCriticalDamage, AdditionalCriticalDamage, 1f, Parameter.CriticalStrikeDamage);
            CurrentExtraHitChance = CalculateValues(_baseExtraHitChance, AdditionalExtraHitChance, IncreaseExtraHitChance, Parameter.AdditionalStrikeChance);
        }

        private void UpdateIncreaseDamageValues()
        {
            CurrentMaxDamage = CalculateValues(_baseMaxDamage, AdditionalMaxDamage, IncreaseDamage, Parameter.StrikeDamage);
            CurrentMinDamage = CalculateValues(_baseMinDamage, AdditionalMinDamage, IncreaseDamage, Parameter.StrikeDamage);
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
    }
}

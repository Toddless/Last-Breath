namespace Playground.Components
{
    using System;
    using System.Collections.Generic;
    using Godot;
    using Playground.Components.Interfaces;
    using Playground.Script.Abilities.Modifiers;
    using Playground.Script.Enums;
    using Playground.Script.Helpers;

    public class DamageComponent
    {
        // Random range for damage
        private const float From = 0.70f;
        private const float To = 1.30f;
        private float _damage, _criticalChance, _criticalDamage, _additionalHit, _maxCriticalChance = 0.75f;
        private readonly RandomNumberGenerator _rnd = new();
        // here we have base values for damage, critical strike chance and damage etc. This strategy changes if we equip a new weapon. Base strategy is "Unarmed"
        // TODO: Rename strategy
        private IDamageStrategy _strategy;

        public float Damage
        {
            get => _damage * _rnd.RandfRange(From, To);
            private set
            {
                if (ObservableProperty.SetProperty(ref _damage, value))
                {
                    // TODO: Raise event with new value to show on UI
                }
            }
        }

        public float CriticalChance
        {
            get => _criticalChance;
            private set
            {
                if (ObservableProperty.SetProperty(ref _criticalChance, value))
                {
                    // TODO: Raise event with new value to show on UI
                }
            }
        }

        public float CriticalDamage
        {
            get => _criticalDamage;
            private set
            {
                if (ObservableProperty.SetProperty(ref _criticalDamage, value))
                {
                    // TODO: Raise event with new value to show on UI
                }
            }
        }

        public float AdditionalHit
        {
            get => _additionalHit;
            private set
            {
                if (ObservableProperty.SetProperty(ref _additionalHit, value))
                {
                    // TODO: Raise event with new value to show on UI
                }
            }
        }

        public DamageComponent(IDamageStrategy strategy)
        {
            _strategy = strategy;
        }

        public event Action? StrategyChanges;

        // TODO: on change strategy i need to recalculate modifiers
        public void ChangeStrategy(IDamageStrategy strategy)
        {
            _strategy = strategy;
            StrategyChanges?.Invoke();
        }

        public void OnParameterChanges(Parameter parameter, List<IModifier> modifiers)
        {
            switch (parameter)
            {
                case Parameter.StrikeDamage:
                    Damage = Calculations.CalculateFloatValue(_strategy.GetDamage(), modifiers);
                    break;
                case Parameter.CriticalStrikeChance:
                    CriticalChance = Mathf.Min(Calculations.CalculateFloatValue(_strategy.GetBaseCriticalChance(), modifiers), _maxCriticalChance);
                    break;
                case Parameter.CriticalStrikeDamage:
                    CriticalDamage = Calculations.CalculateFloatValue(_strategy.GetBaseCriticalDamage(), modifiers);
                    break;
                case Parameter.AdditionalStrikeChance:
                    AdditionalHit = Calculations.CalculateFloatValue(_strategy.GetBaseExtraHitChance(), modifiers);
                    break;
                default:
                    break;
            }
        }

        public bool IsCrit() => _rnd.Randf() <= CriticalChance;
    }
}

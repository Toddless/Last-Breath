namespace LastBreath.Components
{
    using System;
    using System.Collections.Generic;
    using Core.Enums;
    using Core.Interfaces.Components;
    using Godot;
    using LastBreath.Script.Helpers;

    public class DamageComponent : IDamageComponent
    {
        // Random range for damage
        private const float From = 0.9f;
        private const float To = 1.1f;
        private float _damage, _criticalChance, _criticalDamage, _additionalHit, _maxCriticalChance = 0.75f, _maxAdditionalHitChance = 0.9f;
        private readonly RandomNumberGenerator _rnd = new();
        // here we have base values for damage, critical strike chance and damage etc. This strategy changes if we equip a new weapon. Base strategy is "Unarmed"
        // TODO: Rename strategy
        private IDamageStrategy _strategy;
        private Dictionary<Parameter, Func<float, IModifiersChangedEventArgs, float>> _overrideCalculations = [];

        public event Action<Parameter, float>? PropertyValueChanges;

        public float Damage
        {
            get => _damage * _rnd.RandfRange(From, To);
            private set
            {
                if (ObservableProperty.SetProperty(ref _damage, value))
                {
                    // TODO: Raise event with new value to show on UI
                    PropertyValueChanges?.Invoke(Parameter.Damage, value);
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
                    PropertyValueChanges?.Invoke(Parameter.CriticalChance, value);
                }
            }
        }

        public float MaxCriticalChance
        {
            get => _maxCriticalChance;
            private set
            {
                if (ObservableProperty.SetProperty(ref _maxCriticalChance, value))
                {
                    PropertyValueChanges?.Invoke(Parameter.MaxCriticalChance, value);
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
                    PropertyValueChanges?.Invoke(Parameter.CriticalDamage, value);
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
                    PropertyValueChanges?.Invoke(Parameter.AdditionalHitChance, value);
                }
            }
        }

        public float MaxAdditionalHit
        {
            get => _maxAdditionalHitChance;
            private set
            {
                if (ObservableProperty.SetProperty(ref _maxAdditionalHitChance, value))
                {
                    PropertyValueChanges?.Invoke(Parameter.MaxAdditionalHitChance, value);
                }
            }
        }

        public DamageComponent(IDamageStrategy strategy)
        {
            _strategy = strategy;
        }


        // TODO: on change strategy i need to recalculate modifiers
        public void ChangeStrategy(IDamageStrategy strategy)
        {
            _strategy = strategy;
        }

        public void OnParameterChanges(object? sender, IModifiersChangedEventArgs args)
        {
            switch (args.Parameter)
            {
                case Parameter.Damage:
                    Damage = CalculateDamage(args);
                    break;
                case Parameter.CriticalChance:
                    CriticalChance = CalculateCriticaChance(args);
                    break;
                case Parameter.CriticalDamage:
                    CriticalDamage = CalculateCriticalDamage(args);
                    break;
                case Parameter.AdditionalHitChance:
                    AdditionalHit = CalculateAdditionalChance(args);
                    break;
                case Parameter.MaxCriticalChance:
                    MaxCriticalChance = CalculateMaxCriticalChance(args);
                    break;
                case Parameter.MaxAdditionalHitChance:
                    MaxAdditionalHit = CalculateMaxAdditionalChance(args);
                    break;
                default:
                    break;
            }
        }

        public void AddOverrideFuncForParameter(Func<float, IModifiersChangedEventArgs, float> newFunc, Parameter parameter)
        {
            if (!_overrideCalculations.TryAdd(parameter, newFunc))
            {
                // TODO Log
            }
        }


        public void RemoveOverrideFuncForParameter(Parameter parameter)
        {
            if (!_overrideCalculations.Remove(parameter))
            {
                // TODO Log
            }
        }

        private float CalculateMaxAdditionalChance(IModifiersChangedEventArgs args)
        {
            if (_overrideCalculations.TryGetValue(Parameter.MaxAdditionalHitChance, out var func)) return func.Invoke(_strategy.GetBaseExtraHitChance(), args);
            return Calculations.CalculateFloatValue(_maxAdditionalHitChance, args.Modifiers);
        }

        private float CalculateMaxCriticalChance(IModifiersChangedEventArgs args)
        {
            if (_overrideCalculations.TryGetValue(Parameter.MaxCriticalChance, out var func)) return func.Invoke(_strategy.GetBaseExtraHitChance(), args);
            return Calculations.CalculateFloatValue(_maxCriticalChance, args.Modifiers);
        }

        private float CalculateAdditionalChance(IModifiersChangedEventArgs args)
        {
            if (_overrideCalculations.TryGetValue(Parameter.AdditionalHitChance, out var func)) return func.Invoke(_strategy.GetBaseExtraHitChance(), args);
            return Mathf.Min(Calculations.CalculateFloatValue(_strategy.GetBaseExtraHitChance(), args.Modifiers), _maxAdditionalHitChance);
        }

        private float CalculateCriticalDamage(IModifiersChangedEventArgs args)
        {
            if (_overrideCalculations.TryGetValue(Parameter.CriticalDamage, out var func)) return func.Invoke(_strategy.GetBaseCriticalDamage(), args);
            return Calculations.CalculateFloatValue(_strategy.GetBaseCriticalDamage(), args.Modifiers);
        }

        private float CalculateCriticaChance(IModifiersChangedEventArgs args)
        {
            if (_overrideCalculations.TryGetValue(Parameter.CriticalChance, out var func)) return func.Invoke(_strategy.GetBaseCriticalChance(), args);
            return Mathf.Min(Calculations.CalculateFloatValue(_strategy.GetBaseCriticalChance(), args.Modifiers), _maxCriticalChance);
        }

        private float CalculateDamage(IModifiersChangedEventArgs args)
        {
            if (_overrideCalculations.TryGetValue(Parameter.Damage, out var func)) return func.Invoke(_strategy.GetDamage(), args);
            return Calculations.CalculateFloatValue(_strategy.GetDamage(), args.Modifiers);
        }
    }
}

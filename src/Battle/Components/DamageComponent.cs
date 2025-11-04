namespace Battle.Components
{
    using System;
    using System.Collections.Generic;
    using Core.Enums;
    using Core.Interfaces.Components;
    using Godot;

    internal class DamageComponent : IDamageComponent
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
                
            }
        }

        public float CriticalChance
        {
            get => _criticalChance;
            private set
            {
               
            }
        }

        public float MaxCriticalChance
        {
            get => _maxCriticalChance;
            private set
            {
              
            }
        }

        public float CriticalDamage
        {
            get => _criticalDamage;
            private set
            {
              
            }
        }

        public float AdditionalHit
        {
            get => _additionalHit;
            private set
            {
                
            }
        }

        public float MaxAdditionalHit
        {
            get => _maxAdditionalHitChance;
            private set
            {
               
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

    }
}

namespace Playground.Components
{
    using System;
    using Godot;
    using Playground.Components.Interfaces;
    using Playground.Script.Enums;

    public class DamageComponent
    {
        // Random range for damage
        private const float From = 0.70f;
        private const float To = 1.30f;
        private float _damage, _criticalChance, _criticalDamage, _additionalHit;
        private readonly RandomNumberGenerator _rnd = new();
        // here we have base values for damage, critical strike chance and damage etc. This strategy changes if we equip a new weapon. Base strategy is "Unarmed"
        // TODO: Rename strategy
        private IDamageStrategy _strategy;
        private readonly ModifierManager _modifierManager;

        public float Damage => _damage * _rnd.RandfRange(From, To);
        public float CriticalChance => _criticalChance;
        public float CriticalDamage => _criticalDamage;
        public float AdditionalHit => _additionalHit;

        public DamageComponent(IDamageStrategy strategy, ModifierManager modifier)
        {
            _strategy = strategy;
            _modifierManager = modifier;
            _modifierManager.ParameterModifiersChanged += OnParameterChanges;
        }

        public event Action? StrategyChanges;

        // TODO: on change strategy i need to recalculate modifiers
        public void ChangeStrategy(IDamageStrategy strategy)
        {
            _strategy = strategy;
            StrategyChanges?.Invoke();
        }

        private void OnParameterChanges(Parameter parameter)
        {
            switch (parameter)
            {
                case Parameter.StrikeDamage:
                    _damage = Calculations.CalculateFloatValue(_strategy.GetDamage(), _modifierManager.GetCombinedModifiers(parameter));
                    break;
                case Parameter.CriticalStrikeChance:
                    _criticalChance = Calculations.CalculateFloatValue(_strategy.GetBaseCriticalChance(), _modifierManager.GetCombinedModifiers(parameter));
                    break;
                case Parameter.CriticalStrikeDamage:
                    _criticalDamage = Calculations.CalculateFloatValue(_strategy.GetBaseCriticalDamage(), _modifierManager.GetCombinedModifiers(parameter));
                    break;
                case Parameter.AdditionalStrikeChance:
                    _additionalHit = Calculations.CalculateFloatValue(_strategy.GetBaseExtraHitChance(), _modifierManager.GetCombinedModifiers(parameter));
                    break;
                default:
                    break;
            }
        }
    }
}

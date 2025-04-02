namespace Playground.Components
{
    using System;
    using Godot;
    using Playground.Components.Interfaces;
    using Playground.Script.Enums;

    public class DamageComponent(IDamageStrategy strategy, ModifierManager modifier)
    {
        // Random range for damage
        private const float From = 0.85f;
        private const float To = 1.15f;
        private readonly RandomNumberGenerator _rnd = new();
        // here we have base values for damage, critical strike chance and damage etc. This strategy changes if we equip a new weapon. Base strategy is "Unarmed"
        // TODO: Rename strategy
        private IDamageStrategy _strategy = strategy;
        private readonly ModifierManager _modifierManager = modifier;

        public event Action? StrategyChanges;

        // TODO: on change strategy i need to recalculate modifiers
        public void ChangeStrategy(IDamageStrategy strategy)
        {
            _strategy = strategy;
            StrategyChanges?.Invoke();
        }
        /// <summary>
        /// Flat damage between min and max damage range after all bonuses calculation 
        /// </summary>
        /// <returns>Round int</returns>
        public int GetFlatDamage() => _modifierManager.CalculateIntValue(_rnd.RandfRange(From, To) * _strategy.GetDamage(), Parameter.StrikeDamage);
        /// <summary>
        /// Critical chance after all bonuses calculation
        /// </summary>
        /// <returns></returns>
        public float GetCriticalChance() => _modifierManager.CalculateFloatValue(_strategy.GetBaseCriticalChance(), Parameter.CriticalStrikeChance);
        /// <summary>
        /// Additional hit chance after all bonuses calculation 
        /// </summary>
        /// <returns></returns>
        public float GetAdditionalHitChance() => _modifierManager.CalculateFloatValue(_strategy.GetBaseExtraHitChance(), Parameter.AdditionalStrikeChance);
        /// <summary>
        /// Get critical damage factor after all bonuses calculation
        /// </summary>
        /// <returns></returns>
        public float GetCriticalDamage() => _modifierManager.CalculateFloatValue(_strategy.GetBaseCriticalDamage(), Parameter.CriticalStrikeDamage);
    }
}

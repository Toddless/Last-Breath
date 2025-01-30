namespace Playground.Components
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Linq;
    using Playground.Script.Effects.Interfaces;
    using Playground.Script.Enums;
    using Playground.Script.Helpers;

    public class EffectManager : ObservableObject
    {
        private ObservableCollection<IEffect> _effects;
        public Action<float>? TakeDamage { get; set; }
        public Action<float>? Heal { get; set; }
        public Action? UpdateProperties { get; set; }

        public ObservableCollection<IEffect> Effects
        {
            get => _effects;
            set => SetProperty(ref _effects, value);
        }

        public EffectManager(ObservableCollection<IEffect> effects)
        {
            _effects = effects;
            Effects.CollectionChanged += ModifyStatsWithEffect;
        }


        protected void ModifyStatsWithEffect(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add || e.Action == NotifyCollectionChangedAction.Remove)
                UpdateProperties?.Invoke();
        }

        public float CalculateValues(float baseValue, float AdditionalValue, float increaseModifier, Parameter parameter)
        {
            float debufSum = 1;
            float bufSum = 1;
            // This loop is faster than the linq expression.
            // Perhaps the linq expression was poorly defined by me.
            foreach (var effect in Effects!.Where(x => x.Parameter == parameter))
            {
                if (effect.EffectType == EffectType.Debuff)
                    debufSum += effect.Modifier;
                if (effect.EffectType == EffectType.Buff)
                    bufSum += effect.Modifier;
            }
            return bufSum * Math.Max(0, debufSum) * ((baseValue + AdditionalValue) * increaseModifier);
        }

        // I need to call this method each enemy`s turn, so don`t forget to subscribe
        public void HandleAppliedEffects()
        {
            foreach (var group in Effects.GroupBy(effect => effect.EffectType))
            {
                if (group.Key == EffectType.Regeneration)
                    Heal?.Invoke(group.Sum(x => x.Modifier));
                if (group.Key == EffectType.Poison || group.Key == EffectType.Bleeding)
                    TakeDamage?.Invoke(group.Sum(x => x.Modifier));
            }
            RemoveExpiredEffects();
        }

        private void RemoveExpiredEffects()
        {
            for (int i = Effects.Count - 1; i >= 0; i--)
            {
                if (--Effects[i].Duration <= 0)
                    Effects.RemoveAt(i);
            }
        }

        public void OnAddAbility(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action != NotifyCollectionChangedAction.Add && e.Action != NotifyCollectionChangedAction.Remove)
                return;
            if (e.NewItems != null)
                AddNewAbilitiesEffects(e.NewItems.OfType<IAbility>());
            if (e.OldItems != null)
                RemoveOldAbilitiesEffects(e.OldItems.OfType<IAbility>());
        }

        private void AddNewAbilitiesEffects(IEnumerable<IAbility> abilities)
        {
            foreach (var effect in abilities.SelectMany(ability => ability.Effects))
                Effects?.Add(effect);
        }

        private void RemoveOldAbilitiesEffects(IEnumerable<IAbility> abilities)
        {
            foreach (var effect in abilities.SelectMany(ability => ability.Effects))
                Effects?.Remove(effect);
        }
    }
}

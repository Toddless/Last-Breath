namespace Playground.Components
{
    using System;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Linq;
    using Playground.Components.Interfaces;
    using Playground.Script.Effects.Interfaces;
    using Playground.Script.Enums;
    using Playground.Script.Helpers;

    public abstract class ComponentBase : ObservableObject, IGameComponent, IDisposable
    {
        private ObservableCollection<IEffect> _effects;
        private bool _disposed;

        public ObservableCollection<IEffect> Effects
        {
            get => _effects;
            set => SetProperty(ref _effects, value);
        }

        protected ComponentBase(ObservableCollection<IEffect> appliedEffects)
        {
            //i need reference to all effects on character
            _effects = appliedEffects;
            Effects.CollectionChanged += ModifyStatsWithEffect;
        }

        public virtual void HandleAppliedEffects()
        {
            if (Effects == null || Effects.Count <= 0)
                return;

            var effectsToRemove = Effects.Where(effect => --effect.Duration == 0).ToList();
            effectsToRemove.ForEach(effect => Effects.Remove(effect));
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                OnDisposeManagedResources();
            }

            OnDisposeUnmanagedResources();

            _disposed = true;
        }

        protected virtual void OnDisposeManagedResources()
        {
            Effects.CollectionChanged -= ModifyStatsWithEffect;
        }

        protected virtual void OnDisposeUnmanagedResources()
        {
        }

        protected void ModifyStatsWithEffect(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // i don´t really know what should be updated, so i update just all properties
            if (e.Action is NotifyCollectionChangedAction.Add || e.Action is NotifyCollectionChangedAction.Remove)
                UpdateValues();
        }

        // this will be called each time some of "Current-" property is needed
        protected float CalculateValues(float baseValue, float AdditionalValue, float increaseModifier, Parameter stat)
        {
            return GetEffectModifier(stat, EffectType.Buff) * GetEffectModifier(stat, EffectType.Debuff) * ((baseValue + AdditionalValue) * increaseModifier);
        }

        protected float GetEffectModifier(Parameter stat, EffectType type)
        {
            if (Effects?.Count <= 0)
                return 1;
            float effectSum = 1;
            // This loop is faster than the linq expression.
            // Perhaps the linq expression was poorly defined by me.
            foreach (var debuff in Effects!.Where(x => x.EffectType == type && x.Stat == stat))
            {
                effectSum += debuff.Modifier;
            }
            return effectSum;
        }

        protected virtual void UpdateValues()
        {

        }

        // DONT FORGET TO CALL THIS, OTHERWISE MAY CAUSE MEMORY LEAK
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ComponentBase()
        {
            Dispose(false);
        }
    }
}

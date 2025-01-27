namespace Playground.Components
{
    using System;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Linq;
    using Playground.Components.Interfaces;
    using Playground.Script.Enums;
    using Playground.Script.Helpers;
    using Playground.Script.Passives;

    public abstract class ComponentBase : ObservableObject, IGameComponent, IDisposable
    {
        private ObservableCollection<IEffect>? _effects;
        private bool _disposed;

        public ObservableCollection<IEffect>? Effects
        {
            get => _effects;
            set => SetProperty(ref _effects, value);
        }

        protected ComponentBase(ObservableCollection<IEffect>? appliedEffects = default)
        {
            // here i need reference to all effects on character
            _effects = appliedEffects;
            Effects ??= [];
            Effects.CollectionChanged += ApplyEffect;
        }

        public void HandleEffectsDuration()
        {
            if (Effects == null || Effects.Count <= 0)
                return;
            foreach (var effect in Effects)
            {
                effect.Duration--;
                if (effect.Duration == 0)
                {
                    Effects?.Remove(effect);
                }
            }
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
            if (Effects != null)
            {
                Effects.CollectionChanged -= ApplyEffect;
                Effects = null;
            }
        }

        protected virtual void OnDisposeUnmanagedResources()
        {
        }

        protected void ApplyEffect(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // i don´t really know what should be updated, so i update just all properties
            if (e.Action is NotifyCollectionChangedAction.Add || e.Action is NotifyCollectionChangedAction.Remove)
                UpdateValues();
        }

        // this will be called each time some of "Current-" property is needed
        protected float CalculateValues(float baseValue, float AdditionalValue, float increaseModifier, Stats stat)
        {
            return GetEffectModifier(stat, EffectType.Buff) * GetEffectModifier(stat, EffectType.Debuff) * ((baseValue + AdditionalValue) * increaseModifier);
        }

        protected float GetEffectModifier(Stats stat, EffectType type)
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

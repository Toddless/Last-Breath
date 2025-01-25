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
        private ObservableCollection<IEffect>? _effects = [];
        private bool _disposed;

        public ObservableCollection<IEffect>? Effects
        {
            get => _effects;
            set => SetProperty(ref _effects, value);
        }
        protected ComponentBase()
        {
            Effects ??= [];
            Effects.CollectionChanged += ApplyEffect;
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
                Effects.CollectionChanged -= ApplyEffect;
            Effects = null;
        }
        protected virtual void OnDisposeUnmanagedResources()
        {
        }
        protected void ApplyEffect(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action is NotifyCollectionChangedAction.Add || e.Action is NotifyCollectionChangedAction.Remove)
                UpdateValues();
        }
        protected float CalculateValues(float baseValue, float AdditionalValue, float increaseModifier, Stats stat)
        {
            return GetEffectModifier(stat, EffectType.Buff) * GetEffectModifier(stat, EffectType.Debuff) * ((baseValue + AdditionalValue) * increaseModifier);
        }
        protected float GetEffectModifier(Stats stat, EffectType type)
        {
            if (Effects?.Count <= 0)
                return 1;
            float effectSumm = 1;
            foreach (var debuff in Effects!.Where(x => x.EffectType == type && x.Stat == stat))
            {
                effectSumm += debuff.Modifier;
            }
            return effectSumm;
        }
        protected virtual void UpdateValues()
        {

        }
        // DONT FORGET TO CALL THIS, OTHERWISE MEMORY LEAK
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

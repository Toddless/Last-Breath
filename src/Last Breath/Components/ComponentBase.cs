namespace Playground.Components
{
    using System;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Linq;
    using Playground.Components.EffectTypeHandlers;
    using Playground.Components.Interfaces;
    using Playground.Script.Effects.Interfaces;
    using Playground.Script.Enums;
    using Playground.Script.Helpers;

    public abstract class ComponentBase : ObservableObject, IGameComponent, IDisposable
    {
        private ObservableCollection<IEffect> _effects;
        private readonly IEffectHandlerFactory? _effectHandlerFactory;

        private bool _disposed;


        public IEffectHandlerFactory? EffectHandlerFactory => _effectHandlerFactory;

        public ObservableCollection<IEffect> Effects
        {
            get => _effects;
            set => SetProperty(ref _effects, value);
        }

        protected ComponentBase(ObservableCollection<IEffect> appliedEffects, IEffectHandlerFactory? effectHandlerFactory)
        {
            //i need reference to all effects on character
            _effectHandlerFactory = effectHandlerFactory;
            _effectHandlerFactory ??= new EffectHandlerFactory();
            _effects = appliedEffects;
            Effects.CollectionChanged += ModifyStatsWithEffect;
        }

        public virtual void HandleAppliedEffects()
        {
            if (Effects.Count <= 0)
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

        protected virtual void UpdateProperty(ref float field, float newValue, Action<float> setter)
        {
            if (field != newValue)
            {
                field = newValue;
                setter(field);
            }
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
            float debufSum = 1;
            float bufSum = 1;
            // This loop is faster than the linq expression.
            // Perhaps the linq expression was poorly defined by me.
            foreach (var effect in Effects!.Where(x => x.Stat == stat))
            {
                if (effect.EffectType == EffectType.Debuff)
                    debufSum += effect.Modifier;
                if (effect.EffectType == EffectType.Buff)
                    bufSum += effect.Modifier;
            }
            return bufSum * debufSum * ((baseValue + AdditionalValue) * increaseModifier);
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

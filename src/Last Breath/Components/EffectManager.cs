namespace Playground.Components
{
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Linq;
    using Playground.Components.EffectTypeHandlers;
    using Playground.Script.Effects.Interfaces;
    using Playground.Script.Enums;

    public class EffectManager
    {
        private ObservableCollection<IEffect> _effects;
        public IHandleEffectTypeStrategy? HandleEffectStrategy { get; set; }

        public ObservableCollection<IEffect> Effects
        {
            get => _effects;
            set => _effects = value;
        }

        public EffectManager(ObservableCollection<IEffect> effects)
        {
            _effects = effects;
        }

        public event NotifyCollectionChangedEventHandler EffectsChanged
        {
            add => _effects.CollectionChanged += value;
            remove => _effects.CollectionChanged -= value;
        }

        public void HandleAppliedEffects()
        {
            if (Effects.Count <= 0)
                return;
            foreach (var group in Effects.GroupBy(effect => effect.EffectType))
            {
                switch (group.Key)
                {
                    case EffectType.Regeneration:
                        HandleEffectStrategy?.HandleEffectType(group);
                        break;

                    case EffectType.Poison:
                        HandleEffectStrategy?.HandleEffectType(group);
                        break;

                    case EffectType.Bleeding:
                        HandleEffectStrategy?.HandleEffectType(group);
                        break;
                }
            }
            RemoveExpiredEffects();
        }

        private void RemoveExpiredEffects()
        {
            var effectsToRemove = Effects.Where(effect => --effect.Duration == 0).ToList();
            effectsToRemove.ForEach(effect => Effects.Remove(effect));
        }

        public float CalculateValues(float baseValue, float AdditionalValue, float increaseModifier, Parameter stat)
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
    }
}

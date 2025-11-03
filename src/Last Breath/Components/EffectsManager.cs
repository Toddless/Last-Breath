namespace LastBreath.Components
{
    using System.Collections.Generic;
    using System.Linq;
    using Core.Enums;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Components;
    using Core.Interfaces.Entity;
    using Utilities;

    public class EffectsManager(IEntity owner) : IEffectsManager
    {
        // all effects from equip, passive skills etc.
        private readonly List<IEffect> _permanentEffects = [];
        // temporary effects will be getting only in figth
        private readonly List<IEffect> _temporaryEffects = [];
        private readonly IEntity _owner = owner;

        public void AddPermanentEffect(IEffect effect) => AddEffects(effect, _permanentEffects);

        public void AddTemporaryEffect(IEffect effect) => AddEffects(effect, _temporaryEffects);

        public void RemoveEffect(IEffect effect)
        {
            var allEffects = GetCombinedEffects();
            if (!allEffects.Contains(effect))
            {
                Tracker.TrackError($"Trying to remove an effect that doesn't exist in the list.", this);
                return;
            }
            effect.OnRemove(_owner);
            allEffects.Remove(effect);
        }

        public void UpdateEffects()
        {
            foreach (var effect in GetCombinedEffects())
            {
                effect.OnTick(_owner);
                if (effect.Expired) RemoveEffect(effect);
            }
        }

        public void RemoveEffectByType(Effects effect)
        {
            var effectsToRemove = GetCombinedEffects().Where(x => x.Effect == effect);

            foreach (var eff in effectsToRemove)
            {
                eff.OnRemove(_owner);
                GetCombinedEffects().Remove(eff);
            }
        }

        public void ClearAllTemporaryEffects() => _temporaryEffects.Clear();

        public bool IsEffectApplied(Effects effect) => GetCombinedEffects().Any(x => x.Effect == effect);

        private void AddEffects(IEffect effect, List<IEffect> list)
        {
            if (list.Contains(effect))
            {
                var existingEffect = list.First(x => x.Effect == effect.Effect);
                existingEffect.OnStacks(effect);
                //existingEffect.OnTick(_owner);
            }
            effect.OnApply(_owner);
            list.Add(effect);
        }

        private List<IEffect> GetCombinedEffects() => [.. _permanentEffects, .. _temporaryEffects];
    }
}

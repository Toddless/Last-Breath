namespace Playground.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Playground.Script;
    using Playground.Script.Abilities.Interfaces;
    using Playground.Script.Enums;

    public class EffectsManager(ICharacter owner)
    {
        // TODO: Decide: All effects are temporary and should be removed after fight ends or some of them can be permanent
        private readonly List<IEffect> _permanentEffects = [];
        private readonly List<IEffect> _temporaryEffects = [];
        private readonly ICharacter _owner = owner;

        public void AddPermanentEffect(IEffect effect) => AddEffects(effect, _permanentEffects);

        public void AddTemporaryEffect(IEffect effect) => AddEffects(effect, _temporaryEffects);

        public void RemoveEffect(IEffect effect)
        {
            if (!GetCombinedEffects().Contains(effect))
            {
                // log
                return;
            }
            effect.OnRemove(_owner);
            GetCombinedEffects().Remove(effect);
        }

        public void UpdateEffects()
        {
            // ToArray preventing Collection Modified exception
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

        public void RemoveAllTemporaryEffects() => _temporaryEffects.ForEach(RemoveEffect);
        public bool IsEffectApplied(Type effect) => GetCombinedEffects().Any(x => x.GetType() == effect);

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

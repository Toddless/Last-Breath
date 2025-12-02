namespace Battle.Components
{
    using System;
    using Core.Enums;
    using System.Linq;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Components;
    using System.Collections.Generic;

    public class EffectsComponent(IEntity owner) : IEffectsComponent
    {
        private readonly Dictionary<object, List<IEffect>> _effects = [];

        public IReadOnlyList<IEffect> Effects => _effects.Values.SelectMany(x => x).ToList();

        // TODO: Add an event when effect is removed??
        public void AddEffect(IEffect effect)
        {
            object? source = effect.Source;
            if (source == null) return;
            // TODO: Now we can have same effect multiple times if they have different sources
            if (!_effects.TryGetValue(source, out List<IEffect>? effects))
            {
                effects = [];
                _effects[source] = effects;
            }

            var sameEffects = _effects.Values
                .SelectMany(list => list)
                .Where(existingEffect => existingEffect.Id == effect.Id)
                .OrderBy(x => x.Duration)
                .ToList();

            int maxStacks = Math.Max(sameEffects.DefaultIfEmpty().Max(x => x?.MaxStacks ?? 0), effect.MaxStacks);

            // replace oldest or weakest effect
            if (sameEffects.Count == maxStacks)
            {
                var oldest = effects.FirstOrDefault(old => old.Duration < effect.Duration && effect.IsStronger(old)) ?? effects.First();
                effects.Remove(oldest);
            }

            effects.Add(effect);
        }

        public void RemoveEffect(IEffect effect)
        {
            object? source = effect.Source;
            if (source == null) return;
            _effects.TryGetValue(source, out List<IEffect>? effects);
            effects?.Remove(effect);

            if (effects?.Count == 0) _effects.Remove(source);
        }

        public void RemoveEffectByStatus(StatusEffects status)
        {
            foreach (var list in _effects.Values)
                list.RemoveAll(x => x.Status == status);
        }

        public void RemoveAllEffects()
        {
            foreach (var effect in _effects.Values.SelectMany(x => x))
                effect.Remove(owner);
        }

        public void TriggerTurnEnd()
        {
            foreach (var effect in GetEffects())
                effect.OnTurnEnd(owner);
        }

        public void TriggerTurnStart()
        {
            foreach (var effect in GetEffects())
                effect.OnTurnStart(owner);
        }

        public void TriggerBeforeAttack(IAttackContext context)
        {
            foreach (IEffect effect in GetEffects())
                effect.OnBeforeAttack(owner, context);
        }

        public void TriggerAfterAttack(IAttackContext context)
        {
            foreach (IEffect effect in GetEffects())
                effect.OnAfterAttack(owner, context);
        }

        private List<IEffect> GetEffects()
        {
            var effects = new List<IEffect>();
            foreach (var permanentEffect in _effects.Values)
                effects.AddRange(permanentEffect);

            return effects;
        }
    }
}

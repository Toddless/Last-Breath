namespace Battle.Internal.Components
{
    using System;
    using Godot;
    using Source;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Data;
    using Core.Enums;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Components;
    using Core.Interfaces.Entity;

    public class EffectsComponent(IFightable owner) : IEffectsComponent
    {
        private readonly Dictionary<string, List<IEffect>> _effectsBySource = [];
        // Global application order across all sources: stacking limits and "oldest stack" eviction use this list.
        private readonly List<IEffect> _orderedEffects = [];
        private readonly List<DotTick> _dotTicks = [];
        public IReadOnlyList<IEffect> Effects => _orderedEffects;
        public event Action<IEffect>? EffectAdded;
        public event Action<IEffect>? EffectRemoved;

        public IEnumerable<IEffect> GetBy(Func<IEffect, bool> predicate) => _effectsBySource.Values.ToList().SelectMany(list => list.Where(predicate));
        public IEnumerable<IEffect> GetBySource(string source) => _effectsBySource.GetValueOrDefault(source, []);

        public void RegisterDotTick(DotTick tick) => _dotTicks.Add(tick);

        public void AddEffect(IEffect newEffect)
        {
            string source = newEffect.Source;
            if (string.IsNullOrWhiteSpace(source)) return;

            var effectsBySource = GetEffectsForSource(source);

            // MaxStacks is a per-target limit: count same-id effects across ALL sources
            var globalOnTarget = FindSameEffects(newEffect.Id, _orderedEffects);

            ProcessEffectStacking(effectsBySource, globalOnTarget, newEffect);
        }

        public void RemoveEffect(IEffect effect)
        {
            string source = effect.Source;
            if (string.IsNullOrWhiteSpace(source)) return;
            _effectsBySource.TryGetValue(source, out List<IEffect>? effects);
            effects?.Remove(effect);
            _orderedEffects.Remove(effect);
            _dotTicks.RemoveAll(dot => dot.Source == effect.InstanceId);
            if (effects?.Count == 0) _effectsBySource.Remove(source);
            EffectRemoved?.Invoke(effect);
        }

        public void RemoveEffectByStatus(StatusEffects status)
        {
            foreach (var effect in _orderedEffects.Where(x => x.Status == status).ToList())
                effect.Remove();
        }

        public void RemoveEffectBySource(string source)
        {
            _effectsBySource.TryGetValue(source, out List<IEffect>? effects);
            foreach (var effect in effects ?? [])
                effect.Remove();
        }

        public void RemoveAllEffects()
        {
            foreach (var effect in _orderedEffects.ToList())
                effect.Remove();

            _effectsBySource.Clear();
            _orderedEffects.Clear();
            _dotTicks.Clear();
        }

        public async void TriggerTurnEnd()
        {
            try
            {
                foreach (var effect in GetEffects())
                    effect.TurnEnd();
                await ApplyDotDamage();
            }
            catch (Exception exception)
            {
                GD.Print($"Exception: {exception.Message}");
            }
        }

        public void TriggerTurnStart()
        {
            foreach (var effect in GetEffects())
                effect.TurnStart();
        }

        private List<IEffect> GetEffects() => [.. _orderedEffects];

        private async Task ApplyDotDamage()
        {
            foreach (IGrouping<StatusEffects, DotTick> byStatus in _dotTicks.GroupBy(dot => dot.Status))
            {
                var status = byStatus.Key;
                // Per caster: a dead caster only cancels their own ticks, not everyone else's
                foreach (IGrouping<IFightable, DotTick> byCaster in byStatus.GroupBy(dot => dot.From))
                {
                    if (!byCaster.Key.IsAlive) continue;
                    float totalDamage = byCaster.Sum(dot => dot.Damage);
                    var damageContext = new DamageContext { Source = byCaster.Key, Cause = DamageCause.Effect };
                    damageContext.Add(status.GetDamageType(), totalDamage);
                    await owner.TakeDamage(damageContext);
                }
            }

            _dotTicks.Clear();
        }

        private void ProcessEffectStacking(List<IEffect> sourceEffects, List<IEffect> globalOnTarget, IEffect newEffect)
        {
            bool isSingleStack = newEffect.MaxStacks <= 1;

            if (isSingleStack) HandleSingleStack(sourceEffects, globalOnTarget, newEffect);
            else HandleMultipleStacks(sourceEffects, globalOnTarget, newEffect);
        }

        private void HandleMultipleStacks(List<IEffect> sourceEffects, List<IEffect> globalOnTarget, IEffect newEffect)
        {
            bool hasReachedMaxStacks = globalOnTarget.Count >= newEffect.MaxStacks;

            if (!hasReachedMaxStacks)
            {
                AddNewEffectAndNotify(sourceEffects, newEffect);
                return;
            }

            // Evict the globally oldest stack, regardless of which source applied it
            var oldEffect = _orderedEffects.FirstOrDefault(effect => effect.Id == newEffect.Id);
            if (oldEffect == null) return;

            oldEffect.Remove();
            AddNewEffectAndNotify(sourceEffects, newEffect);
        }

        private void HandleSingleStack(List<IEffect> sourceEffects, List<IEffect> globalOnTarget, IEffect newEffect)
        {
            if (globalOnTarget.Count == 0) AddNewEffectAndNotify(sourceEffects, newEffect);
            else
            {
                // oldest effect
                IEffect existingEffect = globalOnTarget[0];

                if (newEffect.IsStronger(existingEffect))
                {
                    existingEffect.Remove();
                    AddNewEffectAndNotify(sourceEffects, newEffect);
                }
                else existingEffect.Duration = Math.Max(existingEffect.Duration, newEffect.Duration);
            }
        }

        private void AddNewEffectAndNotify(List<IEffect> sourceEffects, IEffect newEffect)
        {
            sourceEffects.Add(newEffect);
            _orderedEffects.Add(newEffect);
            EffectAdded?.Invoke(newEffect);
        }

        private List<IEffect> FindSameEffects(string newEffectId, List<IEffect> effects) => effects.Where(x => x.Id == newEffectId).ToList();

        private List<IEffect> GetEffectsForSource(string source)
        {
            if (_effectsBySource.TryGetValue(source, out List<IEffect>? effects)) return effects;

            effects = [];
            _effectsBySource[source] = effects;
            return effects;
        }
    }
}

namespace Core.Entity.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Battle.Abilities;
    using Context;
    using Data;
    using Entity;
    using Enums;
    using Views;
    using Godot;

    public class EffectsComponent(IFightable owner) : IEffectsComponent
    {
        private readonly Dictionary<string, List<IEffect>> _effectsBySource = [];
        // Global application order across all sources: stacking limits and the standing stack a capped
        // re-application refreshes are both read off this list.
        private readonly List<IEffect> _orderedEffects = [];
        private readonly List<DotTick> _dotTicks = [];
        public IReadOnlyList<IEffect> Effects => _orderedEffects;
        public event Action<IEffect>? EffectAdded;
        public event Action<IEffect>? EffectRemoved;
        public event Action? EffectsChanged;

        /// <summary>One view per effect id: stacks = number of instances, duration = the longest remaining.</summary>
        public IReadOnlyList<EffectView> GetEffectViews() =>
            _orderedEffects
                .GroupBy(effect => effect.Id)
                .Select(group => new EffectView(group.Key, group.First().Icon, group.Count(), group.Max(effect => effect.Duration), group.First().Description, group.First().IsHarmful))
                .ToList();

        public IEnumerable<IEffect> GetBy(Func<IEffect, bool> predicate) => _effectsBySource.Values.ToList().SelectMany(list => list.Where(predicate));
        public IEnumerable<IEffect> GetBySource(string source) => _effectsBySource.GetValueOrDefault(source, []);

        public void RegisterDotTick(DotTick tick) => _dotTicks.Add(tick);

        public bool AddEffect(IEffect newEffect)
        {
            string source = newEffect.Source;
            if (string.IsNullOrWhiteSpace(source)) return false;

            var effectsBySource = GetEffectsForSource(source);

            // MaxStacks is a per-target limit: count same-id effects across ALL sources
            var globalOnTarget = FindSameEffects(newEffect.Id, _orderedEffects);

            return ProcessEffectStacking(effectsBySource, globalOnTarget, newEffect);
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
            EffectsChanged?.Invoke();
        }

        public void RemoveEffectByStatus(StatusEffects status)
        {
            foreach (var effect in _orderedEffects.Where(x => x.Status == status).ToList())
                effect.Remove();
        }

        public void RemoveEffectBySource(string source)
        {
            _effectsBySource.TryGetValue(source, out List<IEffect>? effects);
            foreach (var effect in effects?.ToList() ?? [])
                effect.Remove();
        }

        public void Dispel(EffectPower strength, DispelScope scope)
        {
            foreach (var effect in _orderedEffects.Where(effect => IsDispelled(effect, strength, scope)).ToList())
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
                EffectsChanged?.Invoke();
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
            EffectsChanged?.Invoke();
        }

        private List<IEffect> GetEffects() => [.. _orderedEffects];

        /// <summary>A dispel takes an effect no stronger than itself and never an absolute one, and only
        /// within the half of the list it was aimed at.</summary>
        private static bool IsDispelled(IEffect effect, EffectPower strength, DispelScope scope) =>
            effect.Power != EffectPower.Absolute
            && effect.Power <= strength
            && (scope == DispelScope.Target) == IsBuff(effect);

        /// <summary>Buffs are what neither harms nor damages: an effect ticking damage belongs with the
        /// debuffs whether or not it was flagged harmful, and anything unflagged is a buff.</summary>
        private static bool IsBuff(IEffect effect) => !effect.IsHarmful && effect is not IDamageOverTurnEffect;

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

        private bool ProcessEffectStacking(List<IEffect> sourceEffects, List<IEffect> globalOnTarget, IEffect newEffect)
        {
            bool isSingleStack = newEffect.MaxStacks <= 1;

            return isSingleStack
                ? HandleSingleStack(sourceEffects, globalOnTarget, newEffect)
                : HandleMultipleStacks(sourceEffects, globalOnTarget, newEffect);
        }

        private bool HandleMultipleStacks(List<IEffect> sourceEffects, List<IEffect> globalOnTarget, IEffect newEffect)
        {
            bool hasReachedMaxStacks = globalOnTarget.Count >= newEffect.MaxStacks;

            if (!hasReachedMaxStacks)
            {
                AddNewEffectAndNotify(sourceEffects, newEffect);
                return true;
            }

            // At the ceiling the re-application buys time and nothing else: the pile keeps the stacks it
            // has. Evicting the oldest instead let a freshly laid weak stack knock out a standing strong
            // one — the newcomer is not compared to anybody, so the trade was never in the bearer's favour.
            // The time goes to the stack CLOSEST TO GOING OUT rather than the oldest one laid: an old
            // stack somebody extended can already outlast the newcomer, and refreshing that one would
            // spend the application on nothing while the short stack burned down beside it.
            Refresh(globalOnTarget.MinBy(effect => effect.Duration)!, newEffect);
            return false;
        }

        private bool HandleSingleStack(List<IEffect> sourceEffects, List<IEffect> globalOnTarget, IEffect newEffect)
        {
            if (globalOnTarget.Count == 0)
            {
                AddNewEffectAndNotify(sourceEffects, newEffect);
                return true;
            }

            // oldest effect
            IEffect existingEffect = globalOnTarget[0];

            if (newEffect.IsStronger(existingEffect))
            {
                existingEffect.Remove();
                AddNewEffectAndNotify(sourceEffects, newEffect);
                return true;
            }

            Refresh(existingEffect, newEffect);
            return false;
        }

        /// <summary>A re-application that lays no stack still refreshes the standing one, to the LONGER of
        /// what it has and the full duration the newcomer arrived with — an effect somebody extended is not
        /// cut back down by a plain re-application.</summary>
        private void Refresh(IEffect standing, IEffect incoming)
        {
            int refreshed = Math.Max(standing.Duration, incoming.Duration);
            if (refreshed == standing.Duration) return;

            standing.Duration = refreshed;
            EffectsChanged?.Invoke();
        }

        private void AddNewEffectAndNotify(List<IEffect> sourceEffects, IEffect newEffect)
        {
            sourceEffects.Add(newEffect);
            _orderedEffects.Add(newEffect);
            EffectAdded?.Invoke(newEffect);
            EffectsChanged?.Invoke();
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

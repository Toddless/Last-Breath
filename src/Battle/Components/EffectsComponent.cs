namespace Battle.Components
{
    using Godot;
    using System;
    using Core.Data;
    using Core.Enums;
    using System.Linq;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;
    using System.Threading.Tasks;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Components;
    using System.Collections.Generic;

    public class EffectsComponent(IEntity owner) : IEffectsComponent
    {
        private readonly Dictionary<string, List<IEffect>> _effects = [];
        private readonly List<DotTick> _dotTicks = [];
        public IReadOnlyList<IEffect> Effects => _effects.Values.SelectMany(x => x).ToList();
        public event Action<IEffect>? EffectAdded;
        public event Action<IEffect>? EffectRemoved;

        public IEnumerable<IEffect> GetBy(Func<IEffect, bool> predicate) => _effects.Values.ToList().SelectMany(list => list.Where(predicate));

        public void RegisterDotTick(DotTick tick) => _dotTicks.Add(tick);

        public void AddEffect(IEffect newEffect)
        {
            string source = newEffect.Source;
            if (string.IsNullOrWhiteSpace(source)) return;

            var effects = GetEffectsForSource(source);

            var sameEffects = FindSameEffects(newEffect.Id, effects);

            ProcessEffectStacking(effects, sameEffects, newEffect);
        }

        public void RemoveEffect(IEffect effect)
        {
            string source = effect.Source;
            if (string.IsNullOrWhiteSpace(source)) return;
            _effects.TryGetValue(source, out List<IEffect>? effects);
            effects?.Remove(effect);
            _dotTicks.RemoveAll(dot => dot.Source == effect.Id);
            if (effects?.Count == 0) _effects.Remove(source);
            EffectRemoved?.Invoke(effect);
        }

        public void RemoveEffectByStatus(StatusEffects status)
        {
            foreach (var effect in _effects.Values.SelectMany(x => x).Where(x => x.Status == status))
                effect.Remove();
        }

        public void RemoveEffectBySource(string source)
        {
            _effects.TryGetValue(source, out List<IEffect>? effects);
            foreach (var effect in effects ?? [])
                effect.Remove();
        }

        public void RemoveAllEffects()
        {
            foreach (var effect in _effects.Values.SelectMany(x => x).ToList())
                effect.Remove();

            _effects.Clear();
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

        public void TriggerBeforeAttack(IAttackContext context)
        {
            foreach (IEffect effect in GetEffects())
                effect.BeforeAttack(context);
        }

        public void TriggerAfterAttack(IAttackContext context)
        {
            foreach (IEffect effect in GetEffects())
                effect.AfterAttack(context);
        }

        private List<IEffect> GetEffects()
        {
            var effects = new List<IEffect>();
            foreach (var permanentEffect in _effects.Values)
                effects.AddRange(permanentEffect);

            return effects;
        }

        private async Task ApplyDotDamage()
        {
            foreach (IGrouping<StatusEffects, DotTick> grouping in _dotTicks.GroupBy(dot => dot.Status))
            {
                var from = grouping.Select(x => x.From).First();
                if (!from.IsAlive) continue;
                var status = grouping.Key;
                float totalDamage = grouping.Sum(dot => dot.Damage);
                await owner.TakeDamage(from, totalDamage, status.GetDamageType(), DamageSource.Effect);
            }

            _dotTicks.Clear();
        }

        private void ProcessEffectStacking(List<IEffect> effects, List<IEffect> sameEffects, IEffect newEffect)
        {
            bool isSingleStack = newEffect.MaxMaxStacks <= 1;

            if (isSingleStack) HandleSingleStack(effects, sameEffects, newEffect);
            else HandleMultipleStacks(effects, sameEffects, newEffect);
        }

        private void HandleMultipleStacks(List<IEffect> effects, List<IEffect> sameEffects, IEffect newEffect)
        {
            bool hasReachedMaxStacks = sameEffects.Count >= newEffect.MaxMaxStacks;

            if (!hasReachedMaxStacks)
            {
                AddNewEffectAndNotify(effects, newEffect);
                return;
            }

            int oldestIndex = effects.FindIndex(x => x.Id == newEffect.Id);
            if (oldestIndex < 0) return;

            var oldEffect = effects[oldestIndex];
            oldEffect.Remove();
            AddNewEffectAndNotify(effects, newEffect);
        }

        private void HandleSingleStack(List<IEffect> effects, List<IEffect> sameEffects, IEffect newEffect)
        {
            if (sameEffects.Count == 0) AddNewEffectAndNotify(effects, newEffect);
            else
            {
                IEffect existingEffect = sameEffects[0];

                if (newEffect.IsStronger(existingEffect))
                {
                    existingEffect.Remove();
                    AddNewEffectAndNotify(effects, newEffect);
                }
                else existingEffect.Duration = Math.Max(existingEffect.Duration, newEffect.Duration);
            }
        }

        private void AddNewEffectAndNotify(List<IEffect> effects, IEffect newEffect)
        {
            effects.Add(newEffect);
            EffectAdded?.Invoke(newEffect);
        }

        private static List<IEffect> FindSameEffects(string newEffectId, List<IEffect> effects) => effects.Where(x => x.Id == newEffectId).ToList();

        private List<IEffect> GetEffectsForSource(string source)
        {
            if (_effects.TryGetValue(source, out List<IEffect>? effects)) return effects;

            effects = [];
            _effects[source] = effects;
            return effects;
        }
    }
}

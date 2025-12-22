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
        private readonly Dictionary<object, List<IEffect>> _effects = [];
        private readonly List<DotTick> _dotTicks = [];
        public IReadOnlyList<IEffect> Effects => _effects.Values.SelectMany(x => x).ToList();
        public event Action<IEffect>? EffectAdded;
        public event Action<IEffect>? EffectRemoved;
        public event Action? AllEffectsRemoved;

        public void RegisterDotTick(DotTick tick)
        {
            _dotTicks.Add(tick);
        }

        public void AddEffect(IEffect effect)
        {
            string source = effect.Source;
            if (string.IsNullOrWhiteSpace(source)) return;
            // we can have same effect multiple times if they have different sources
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
            EffectAdded?.Invoke(effect);
        }

        public void RemoveEffect(IEffect effect)
        {
            string source = effect.Source;
            if (string.IsNullOrWhiteSpace(source)) return;
            _effects.TryGetValue(source, out List<IEffect>? effects);
            effects?.Remove(effect);
            _dotTicks.RemoveAll(x => x.Source.InstanceId == effect.InstanceId);
            EffectRemoved?.Invoke(effect);
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
                effect.Remove();
            AllEffectsRemoved?.Invoke();
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

        private async Task ApplyDotDamage()
        {
            foreach (IGrouping<StatusEffects, DotTick> grouping in _dotTicks.GroupBy(dot => dot.Status))
            {
                var status = grouping.Key;
                float totalDamage = grouping.Sum(dot => dot.Damage);
                var from = grouping.Select(x => x.Source).First();
                await owner.TakeDamage(from, totalDamage, status.GetDamageType(), DamageSource.Effect);
            }

            _dotTicks.Clear();
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
    }
}

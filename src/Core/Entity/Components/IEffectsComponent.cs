namespace Core.Entity.Components
{
    using System;
    using System.Collections.Generic;
    using Battle.Abilities;
    using Data;
    using Enums;
    using Views;

    public interface IEffectsComponent
    {
        IReadOnlyList<IEffect> Effects { get; }

        event Action<IEffect>? EffectAdded;
        event Action<IEffect>? EffectRemoved;
        /// <summary>Fires whenever the aggregated effect view list changes (add/remove/duration tick).</summary>
        event Action? EffectsChanged;

        /// <summary>Effects aggregated by id for UI: one entry per effect with stack count and remaining duration.</summary>
        IReadOnlyList<EffectView> GetEffectViews();

        public IEnumerable<IEffect> GetBy(Func<IEffect, bool> predicate);
        public IEnumerable<IEffect> GetBySource(string source);
        void RegisterDotTick(DotTick tick);
        void RemoveEffect(IEffect effect);
        void TriggerTurnEnd();
        void TriggerTurnStart();
        void AddEffect(IEffect newEffect);
        void RemoveEffectByStatus(StatusEffects status);
        void RemoveAllEffects();
        void RemoveEffectBySource(string source);
    }
}

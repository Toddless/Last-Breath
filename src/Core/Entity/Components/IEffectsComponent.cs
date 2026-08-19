namespace Core.Entity.Components
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
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
        /// <summary>The bearer's end of turn: durations first, then the damage they booked. Awaitable
        /// because the damage can kill, and the turn may not be called over before it has.</summary>
        Task TriggerTurnEnd();
        void TriggerTurnStart();
        /// <summary>Returns whether the stacking rules accepted THIS instance (a rejected single-stack
        /// re-application only refreshes the existing effect and returns false).</summary>
        bool AddEffect(IEffect newEffect);
        void RemoveEffectByStatus(StatusEffects status);
        void RemoveAllEffects();
        void RemoveEffectBySource(string source);

        /// <summary>Strips the effects a dispel of this strength reaches: it takes everything at or below
        /// its own <see cref="EffectPower"/> and never an absolute effect. <see cref="DispelScope.Self"/>
        /// takes damaging effects and debuffs, <see cref="DispelScope.Target"/> takes buffs.</summary>
        void Dispel(EffectPower strength, DispelScope scope);
    }
}

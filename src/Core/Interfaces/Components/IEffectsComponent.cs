namespace Core.Interfaces.Components
{
    using System;
    using System.Collections.Generic;
    using Abilities;
    using Battle;
    using Data;
    using Enums;

    public interface IEffectsComponent
    {
        IReadOnlyList<IEffect> Effects { get; }

        event Action<IEffect>? EffectAdded;
        event Action<IEffect>? EffectRemoved;

        public IEnumerable<IEffect> GetBy(Func<IEffect, bool> predicate);
        void RegisterDotTick(DotTick tick);
        void RemoveEffect(IEffect effect);
        void TriggerTurnEnd();
        void TriggerTurnStart();
        void TriggerBeforeAttack(IAttackContext context);
        void TriggerAfterAttack(IAttackContext context);
        void AddEffect(IEffect newEffect);
        void RemoveEffectByStatus(StatusEffects status);
        void RemoveAllEffects();
        void RemoveEffectBySource(string source);
    }
}

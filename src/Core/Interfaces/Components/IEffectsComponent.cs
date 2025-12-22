namespace Core.Interfaces.Components
{
    using Enums;
    using System;
    using Battle;
    using Abilities;
    using Core.Data;
    using System.Collections.Generic;

    public interface IEffectsComponent
    {
        IReadOnlyList<IEffect> Effects { get; }

        event Action<IEffect>? EffectAdded;
        event Action<IEffect>? EffectRemoved;
        event Action? AllEffectsRemoved;

        void RegisterDotTick(DotTick tick);
        void TriggerTurnEnd();
        void TriggerTurnStart();
        void TriggerBeforeAttack(IAttackContext context);
        void TriggerAfterAttack(IAttackContext context);
        void AddEffect(IEffect effect);
        void RemoveEffect(IEffect effect);
        void RemoveEffectByStatus(StatusEffects status);
        void RemoveAllEffects();
    }
}

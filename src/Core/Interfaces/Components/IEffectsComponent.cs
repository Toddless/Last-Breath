namespace Core.Interfaces.Components
{
    using Enums;
    using Battle;
    using Abilities;
    using System.Collections.Generic;

    public interface IEffectsComponent
    {
        IReadOnlyList<IEffect> Effects { get; }

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

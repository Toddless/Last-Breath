namespace Core.Interfaces.Components
{
    using Enums;
    using Abilities;

    public interface IEffectsManager
    {
        void AddPermanentEffect(IEffect effect);
        void AddTemporaryEffect(IEffect effect);
        void ClearAllTemporaryEffects();
        bool IsEffectApplied(StatusEffects statusEffect);
        void RemoveEffect(IEffect effect);
        void RemoveEffectByType(StatusEffects statusEffect);
        void UpdateEffects();
    }
}

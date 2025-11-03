namespace Core.Interfaces.Components
{
    using Core.Enums;
    using Core.Interfaces.Abilities;

    public interface IEffectsManager
    {
        void AddPermanentEffect(IEffect effect);
        void AddTemporaryEffect(IEffect effect);
        void ClearAllTemporaryEffects();
        bool IsEffectApplied(Effects effect);
        void RemoveEffect(IEffect effect);
        void RemoveEffectByType(Effects effect);
        void UpdateEffects();
    }
}

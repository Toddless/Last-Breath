namespace Core.Battle
{
    using Entity;

    /// <summary>
    /// Stat modifiers and passives a stance grants while active. Split on purpose:
    /// modifiers always follow the stance, passives are additionally gated by passive suppression
    /// (Seal of Oblivion) — the owning stance decides when to attach/detach them.
    /// </summary>
    public interface IStanceActivationEffect
    {
        void ApplyModifiers(IFightable owner);
        void RemoveModifiers(IFightable owner);
        void AttachPassives(IFightable owner);
        void DetachPassives(IFightable owner);
    }
}

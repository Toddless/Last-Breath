namespace Core.Interfaces.Abilities
{
    using Core.Enums;
    using Core.Interfaces;
    using Core.Interfaces.Entity;

    public interface IEffect
    {
        Effects Effect { get; }
        IModifierInstance? Modifier { get; }
        int Duration { get; set; }
        int Stacks { get; set; }
        bool Permanent { get; }
        bool Expired { get; }

        void OnApply(IEntity character);
        void OnTick(IEntity character);
        void OnRemove(IEntity character);
        void OnStacks(IEffect newEffect);
    }
}

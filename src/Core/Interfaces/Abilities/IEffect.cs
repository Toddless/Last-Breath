namespace Core.Interfaces.Abilities
{
    using Core.Enums;
    using Core.Interfaces;

    public interface IEffect
    {
        Effects Effect { get; }
        IModifierInstance? Modifier { get; }
        int Duration { get; set; }
        int Stacks { get; set; }
        bool Permanent { get; }
        bool Expired { get; }

        void OnApply(ICharacter character);
        void OnTick(ICharacter character);
        void OnRemove(ICharacter character);
        void OnStacks(IEffect newEffect);
    }
}

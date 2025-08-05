namespace LastBreath.Script.Abilities.Interfaces
{
    using Core.Enums;
    using Core.Interfaces;
    using LastBreath.Script;

    public interface IEffect
    {
        Effects Effect { get; }
        IModifier? Modifier { get; }
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

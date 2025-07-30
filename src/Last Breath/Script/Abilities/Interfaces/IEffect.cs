namespace LastBreath.Script.Abilities.Interfaces
{
    using LastBreath.Script;
    using LastBreath.Script.Abilities.Modifiers;
    using LastBreath.Script.Enums;

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

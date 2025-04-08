namespace Playground.Script.Abilities.Interfaces
{
    using Playground.Script.Enums;

    public interface IAbility
    {
        string Name { get; }
        string Description { get; }
        int Cooldown { get; set; }
        int Cost { get; }
        bool CanActivateOnCaster { get; }
        ResourceType Type { get; }
        void Activate(ICharacter target);
        bool AbilityCanActivate();

        // for now i ignore UI, animations and sounds
        // i should add this later

        // Resource??
        // int   str   dex
        // Mana, Fury, Combo-Points

        // we do not care about abilities in stances for enemies
        // each enemy have only one stance
    }
}

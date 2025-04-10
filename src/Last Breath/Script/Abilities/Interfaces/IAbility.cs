namespace Playground.Script.Abilities.Interfaces
{
    using System;
    using Playground.Script.Enums;

    public interface IAbility
    {
        string Name { get; }
        string Description { get; }
        int Cooldown { get; set; }
        int Cost { get; }
        bool ActivateOnlyOnCaster { get; }
        ResourceType Type { get; }

        event Action? OnCooldown, OnCost, OnTarget;
        void Activate(ICharacter target);
        bool AbilityCanActivate(ICharacter target);
        void UpdateCooldown();

        // for now i ignore UI, animations and sounds
        // i should add this later

        // Resource??
        // int   str   dex
        // Mana, Fury, Combo-Points

        // we do not care about abilities in stances for enemies
        // each enemy have only one stance
    }
}

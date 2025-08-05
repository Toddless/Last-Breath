namespace LastBreath.Script.Abilities.Interfaces
{
    using System;
    using Core.Enums;
    using Godot;
    using LastBreath.Script;

    public interface IAbility
    {
        Texture2D? Icon { get; }
        string Name { get; }
        string Description { get; }
        int Cooldown { get; set; }
        int Cost { get; }
        bool ActivateOnlyOnCaster { get; }
        ResourceType Type { get; }
        ICharacter Target { get; set; }

        event Action? OnCooldown, OnCost, OnTarget, AbilityUpdateState;
        void Activate();
        bool AbilityCanActivate();
        void UpdateCooldown();
        void UpdateState();

        // for now i ignore UI, animations and sounds
        // i should add this later

        // we do not care about abilities in stances for enemies
        // each enemy have only one stance
    }
}

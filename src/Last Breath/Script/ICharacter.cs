namespace Playground.Script
{
    using Playground.Components;
    using Playground.Script.Enums;

    public interface ICharacter
    {
        public HealthComponent? Health { get; }
        public DamageComponent? Damage { get; }
        public DefenseComponent? Defense { get; }
        public EffectsManager Effects {  get; }
        public ModifierManager Modifiers { get; }
        public ResourceManager Resource { get; }
        Stance Stance { get; set; }
        bool CanFight { get; set; }
        bool CanMove { get; set; }

        // TODO: Resources class
        // i need 3 types, for each stance
        // each of resource has its own way to recover
        // but all of them should be recovered at the end of the turn
    }
}

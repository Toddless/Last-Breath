namespace Playground.Script
{
    using Playground.Components;
    using Playground.Script.Enums;

    public interface ICharacter
    {
        public HealthComponent? Health { get; }
        public DamageComponent? Damage { get; }
        public DefenseComponent? Defense { get; }
        Stance Stance { get; set; }
        bool CanFight { get; set; }
        bool CanMove { get; set; }
    }
}

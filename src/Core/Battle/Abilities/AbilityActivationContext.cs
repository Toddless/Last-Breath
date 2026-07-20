namespace Core.Battle.Abilities
{
    using System.Collections.Generic;
    using Enums;
    using Entity;
    using Godot;

    public record AbilityActivationContext : IAbilityActivationContext
    {
        public required IAbility Ability { get; init; }
        public required IFightable Caster { get; init; }
        public required IBattleField Field { get; init; }
        public required RandomNumberGenerator Rnd { get; init; }
        public List<IFightable> Targets { get; init; } = [];

        public float Cost { get; set; }
        public Costs CostType { get; set; }
        public float Cooldown { get; set; }
    }
}

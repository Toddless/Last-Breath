namespace Core.Interfaces.Abilities
{
    using System.Collections.Generic;
    using Battle;
    using Entity;
    using Enums;

    public record AbilityActivationContext : IAbilityActivationContext
    {
        public required IAbility Ability { get; init; }
        public required IFightable Caster { get; init; }
        public required IBattleField Field { get; init; }
        public List<IFightable> Targets { get; init; } = [];

        public float Cost { get; set; }
        public Costs CostType { get; set; }
        public float Cooldown { get; set; }
    }
}

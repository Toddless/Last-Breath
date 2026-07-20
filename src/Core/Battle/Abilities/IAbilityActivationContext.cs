namespace Core.Battle.Abilities
{
    using System.Collections.Generic;
    using Enums;
    using Entity;
    using Godot;

    /// <summary>
    /// Mutable cast context. Built from the ability's current values, then passed through activation
    /// modifiers — ability-scoped first, entity-scoped (ModifierHandler) second — before the cost is
    /// consumed and the cooldown starts. "The first ability each turn is free" is just a modifier
    /// setting <see cref="Cost"/> to zero; no ability decorators involved.
    /// </summary>
    public interface IAbilityActivationContext
    {
        IAbility Ability { get; }
        IFightable Caster { get; }
        IBattleField Field { get; }
        List<IFightable> Targets { get; }

        /// <summary>Rolls for mutators that fire by chance ("X% chance the cast costs nothing"). Lives on the
        /// context like <see cref="IAttackContext.Rnd"/> so a mutator never has to reach for a generator.</summary>
        RandomNumberGenerator Rnd { get; }

        float Cost { get; set; }
        Costs CostType { get; set; }
        float Cooldown { get; set; }
    }
}

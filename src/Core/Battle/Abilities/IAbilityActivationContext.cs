namespace Core.Battle.Abilities
{
    using System.Collections.Generic;
    using Enums;
    using Entity;
    using Entity.Components;

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
        /// context like <see cref="IAttackContext.Rnd"/> so a mutator never has to reach for a generator.
        /// The contract names the abstraction, not an engine type: the running game hands over the Godot
        /// generator, while sandboxes without the engine hand over a pure-C# one.</summary>
        IRandomNumberGenerator Rnd { get; }

        /// <summary>
        /// True when the context estimates effective cost/cooldown for availability checks and UI —
        /// nothing is paid and delivery never runs. Preview contexts carry no <see cref="Field"/>,
        /// no <see cref="Targets"/> and no <see cref="Rnd"/>; chance-based mutators must stay inert
        /// (no roll — availability is pessimistic) and self-consuming mutators must not spend themselves.
        /// </summary>
        bool IsPreview { get; }

        float Cost { get; set; }
        Costs CostType { get; set; }
        float Cooldown { get; set; }
    }
}

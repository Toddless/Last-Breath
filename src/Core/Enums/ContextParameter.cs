namespace Core.Enums
{
    /// <summary>Pipeline knobs items can tune — the context counterpart of <see cref="EntityParameter"/>.
    /// Shares the data-level "parameter" namespace with it, so member names must never collide.
    /// Members are added per concrete design need, not as a status × knob cross-product.</summary>
    public enum ContextParameter
    {
        /// <summary>Scales heals the owner receives ("healing efficiency +15%").</summary>
        HealingEfficiency = 1,

        /// <summary>Extends bleed effects the owner applies ("+1 bleed duration"). Whole-number knob.</summary>
        BleedDuration,

        /// <summary>Scales the per-tick damage of Bleed the owner applies ("+35% bleed damage").</summary>
        BleedDamage,

        /// <summary>Extra burning stacks per application ("applies an additional burning stack"). Whole-number knob.</summary>
        BurningStacks,

        /// <summary>Scales the per-tick damage of burnings the owner applies ("+35% burning damage").</summary>
        BurningDamage,

        /// <summary>        </summary>
        ManaOnHit,

        /// <summary>        </summary>
        HealthOnHit,
    }
}

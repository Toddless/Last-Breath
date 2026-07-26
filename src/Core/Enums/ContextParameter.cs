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

        /// <summary>Scales the per-tick damage of poisons the owner applies — the third of the per-status
        /// DoT knobs (see <see cref="DotDamageBonus"/> for the one that covers all three at once).</summary>
        PoisonDamage,

        /// <summary>        </summary>
        ManaOnHit,

        /// <summary>        </summary>
        HealthOnHit,

        // ------------------------------------------------------------------ mythic marks (2026-07-20)
        // One member per concrete meaning: the data entry carries only a number, so "which element",
        // "which cause" and "which status" live in the binding.

        /// <summary>Converts a share of the owner's PHYSICAL damage into fire/cold/lightning. Three members,
        /// one element each — a pool offers all three and the item wears one.</summary>
        PhysicalToFire,
        PhysicalToCold,
        PhysicalToLightning,

        /// <summary>The owner's ATTACKS skip elemental resistances. A switch: written as a "flag" line.</summary>
        AttacksIgnoreResistances,

        /// <summary>Adds a share of the owner's attack damage back as fire/cold/lightning.</summary>
        AddedFireDamage,
        AddedColdDamage,
        AddedLightningDamage,

        /// <summary>Converts a share of the owner's ATTACK damage into Pure.</summary>
        AttackPureConversion,

        /// <summary>Chance that an activation leaves no cooldown behind.</summary>
        CooldownResetChance,

        /// <summary>Chance that an activation costs nothing, whatever the cost type.</summary>
        FreeCastChance,

        /// <summary>Scales the duration of every effect the owner applies ("doubles it").</summary>
        EffectDurationScale,

        /// <summary>Reduces damage the owner TAKES, split by what caused it.</summary>
        DamageTakenReductionFromAttack,
        DamageTakenReductionFromAbility,
        DamageTakenReductionFromEffect,
        DamageTakenReductionFromPassive,

        /// <summary>Reduces the damage-over-turn components of hits the owner takes.</summary>
        DotDamageTakenReduction,

        /// <summary>Per-status siblings of <see cref="DotDamageTakenReduction"/>: reduce only the matching
        /// DoT component of hits the owner takes ("burning damage taken −20%").</summary>
        BurningDamageTakenReduction,
        PoisonDamageTakenReduction,
        BleedDamageTakenReduction,
    }
}

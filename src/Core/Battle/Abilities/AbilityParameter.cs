namespace Core.Battle.Abilities
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Parameter keys of the ability book: the ones every ability owns and the CONCEPTS more than one of
    /// them shares. A shared key is a concept an ability opts into by registering it — a record standing
    /// on that key works wherever it was registered and is inert everywhere else. Ability-specific keys
    /// live in the ability's own nested <c>Parameters</c> class.
    /// Vocabulary rules, latent duplicates and the reasoning — <c>Docs/PLAN-Augments.md</c>.
    /// </summary>
    public static class AbilityParameter
    {
        public const string Cooldown = nameof(Cooldown);
        public const string CostType = nameof(CostType);
        public const string CostValue = nameof(CostValue);
        public const string Damage = nameof(Damage);
        public const string SpellDamageScale = nameof(SpellDamageScale);
        public const string WeaponDamageScale = nameof(WeaponDamageScale);

        /// <summary>What a cast is paid with. A move RAISING one of these is a bill rather than a gift,
        /// which is the distinction <see cref="AbilityEffectIdentity"/> is drawn on: bills belong to the
        /// record that charges them and are all paid, gifts of a kind compete.</summary>
        public static readonly IReadOnlySet<string> CastPrices =
            new HashSet<string>(StringComparer.Ordinal) { Cooldown, CostValue };

        /// <summary>Fractional increase of the owner's critical chance for this ability: final = owner * (1 + bonus).</summary>
        public const string CriticalChanceBonus = nameof(CriticalChanceBonus);

        /// <summary>Fractional increase of the owner's critical damage for this ability: final = owner and bonus.</summary>
        public const string CriticalDamageBonus = nameof(CriticalDamageBonus);

        /// <summary>How long the buff a cast lays on its CASTER holds, in turns — not what it leaves on
        /// anybody else.</summary>
        public const string Duration = nameof(Duration);

        /// <summary>How many stacks of what a cast lays it lays at once — of its buff or of its debuff,
        /// whichever that ability does; no ability counts both. Never scaled by <see cref="Effectiveness"/>.</summary>
        public const string Stacks = nameof(Stacks);

        /// <summary>
        /// How strongly what a cast lays lands: the multiplier every number of the applied content is
        /// read through, except duration and stacks. The key is the ability's; the multiplier is stamped
        /// on the effect (<see cref="IEffect.Effectiveness"/>) and applied in one place
        /// (<see cref="EffectValue"/>). Owners and record tagging — <c>Docs/PLAN-Augments.md</c>.
        /// </summary>
        public const string Effectiveness = nameof(Effectiveness);

        /// <summary>How many attacks one cast delivers. Not owned by an ability whose series is a RANGE:
        /// one number cannot say which end of it a record is buying.</summary>
        public const string Attacks = nameof(Attacks);

        /// <summary>How many projectiles one cast sends. A counted touch like <see cref="Attacks"/>: each
        /// one is an impact, so raising it multiplies the work of every impact rider the ability wears.</summary>
        public const string ProjectileCount = nameof(ProjectileCount);

        /// <summary>How long the stun a cast puts on its target holds, in turns.</summary>
        public const string StunDuration = nameof(StunDuration);

        /// <summary>How long a poison stack laid by a cast holds, in turns. Owned by the abilities that
        /// poison and LENT to those an augment teaches to (<c>AugmentPoisonOnHit</c>).</summary>
        public const string PoisonDuration = nameof(PoisonDuration);

        /// <summary>The multiplier this cast's own damage is read through. Abilities start from different
        /// bases; that is a base value and no part of the concept.</summary>
        public const string DamageMultiplier = nameof(DamageMultiplier);

        /// <summary>How many times a cast may be spent before it has to be recharged.</summary>
        public const string Charges = nameof(Charges);

        /// <summary>How much a shield granted by a cast holds — the separate defence layer, NOT the
        /// barrier.</summary>
        public const string ShieldStrength = nameof(ShieldStrength);

        /// <summary>The share of MAXIMUM HEALTH a cast's buff adds while it stands. A ceiling, not a
        /// heal: what it moves is how much health the bearer has room for.</summary>
        public const string HealthBonus = nameof(HealthBonus);

        /// <summary>The share of maximum health what a cast lays gives back each turn. A rate over turns,
        /// which is what separates it from a one-off restore and from <see cref="HealthBonus"/>.</summary>
        public const string HealthRegeneration = nameof(HealthRegeneration);

        /// <summary>Where a cast's execute starts killing outright. The UNIT belongs to the ability —
        /// poison stacks for one, a share of health for another — and one number cannot be both, so an
        /// ability whose execute is measured differently keeps its own key rather than joining this one.</summary>
        public const string ExecutionThreshold = nameof(ExecutionThreshold);

        /// <summary>The shortest wait a SHARE-shaped cut of the cooldown may leave; flat cuts written into
        /// an ability's own upgrade pass it deliberately. It holds a reduction back and never raises a
        /// number the data did not ask for, so an instant cast stays instant.</summary>
        public const float MinimumCooldown = 1f;
    }
}

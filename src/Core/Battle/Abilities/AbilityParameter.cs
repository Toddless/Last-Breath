namespace Core.Battle.Abilities
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Keys of the parameters every ability owns. Parameter keys are strings — the same names the
    /// JSON data and the description placeholders use; ability-specific keys live in a nested
    /// <c>Parameters</c> constants class of the ability itself. Rules that hold for a parameter across
    /// the whole book — which of them are prices, how far one may be moved — belong here beside its
    /// key, so a system reading the parameter finds what it is allowed to do with it in one place.
    /// </summary>
    public static class AbilityParameter
    {
        public const string Cooldown = nameof(Cooldown);
        public const string CostType = nameof(CostType);
        public const string CostValue = nameof(CostValue);
        public const string Damage = nameof(Damage);
        public const string SpellDamageScale = nameof(SpellDamageScale);
        public const string WeaponDamageScale = nameof(WeaponDamageScale);

        /// <summary>
        /// What a cast is paid with. An augment may ask for more of either in return for what it offers
        /// — a longer wait for reaching every target, fifty more mana for a longer stun — and nothing
        /// an augment OFFERS is a higher price or a longer wait. That is what makes a move raising one
        /// of these a bill rather than a gift, and it is the one distinction the effect identity is
        /// drawn on (<see cref="AbilityEffectIdentity"/>): bills belong to the record that charges them
        /// and every one of them is paid, while gifts of a kind compete and only the best works.
        /// A parameter that becomes payable has to be named here, or two records charging on it would
        /// waive one another's bill.
        /// </summary>
        public static readonly IReadOnlySet<string> CastPrices =
            new HashSet<string>(StringComparer.Ordinal) { Cooldown, CostValue };

        /// <summary>Fractional increase of the owner's critical chance for this ability: final = owner * (1 + bonus).</summary>
        public const string CriticalChanceBonus = nameof(CriticalChanceBonus);

        /// <summary>Fractional increase of the owner's critical damage for this ability: final = owner and bonus.</summary>
        public const string CriticalDamageBonus = nameof(CriticalDamageBonus);

        /// <summary>
        /// The shortest wait a SHARE-shaped cut of the cooldown may leave: those augments offer a shorter
        /// cooldown and not a cooldown removed, so however deep the share they are written with, the cut
        /// stops here. It binds the decorators that honour it and nothing else — a flat subtraction
        /// written into an ability's own upgrade cuts straight past it, deliberately, because a number
        /// authored for one ability is a decision already taken about that ability's wait.
        /// The floor is on the REDUCTION alone: an ability written to wait for nothing keeps its instant
        /// cast, because a floor may never raise a number the data did not ask for.
        /// </summary>
        public const float MinimumCooldown = 1f;
    }
}

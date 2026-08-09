namespace Core.Battle.Abilities
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Keys of the parameters every ability owns, and of the CONCEPTS more than one of them shares.
    /// Parameter keys are strings — the same names the JSON data and the description placeholders use;
    /// ability-specific keys live in a nested <c>Parameters</c> constants class of the ability itself.
    /// Rules that hold for a parameter across the whole book — which of them are prices, how far one
    /// may be moved — belong here beside its key, so a system reading the parameter finds what it is
    /// allowed to do with it in one place.
    ///
    /// <para><b>A shared key is a concept, and the concept has an owner.</b> A key named here does not
    /// mean every ability carries it: it means that whichever abilities DO carry it mean the same thing
    /// by it, and each of them says so by registering the key in its own
    /// <c>RegisterBaseParameters</c>. That registration is the whole contract — an augment written
    /// against the key works on every ability that made the declaration and is inert on every ability
    /// that did not, without either side naming the other. An ability whose number happens to spell the
    /// same word but means something else registers a key of its own instead, or the augment would move
    /// a number it was never offered for. A key nobody would ever register twice belongs to its
    /// ability, not here.</para>
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
        /// How long the buff a cast lays on its CASTER holds, in turns. The concept is the caster's own
        /// state and not what the cast does to anybody else — an ability that both buffs its owner and
        /// marks its target keeps two separate numbers, and only the first of them is this one.
        /// An augment standing here offers "your buff lasts longer" and means it on every ability that
        /// registered the key, which is what makes one such record worth writing instead of one per
        /// ability. Two records offering it are one offer at two strengths and only the better works.
        /// </summary>
        public const string Duration = nameof(Duration);

        /// <summary>
        /// How many stacks of that buff a cast lays at once. Registered by the abilities whose buff is
        /// counted in stacks; an ability whose buff is a single application does not name it, and an
        /// augment offering more stacks is inert there rather than inventing a stack it has no use for.
        /// </summary>
        public const string Stacks = nameof(Stacks);

        /// <summary>
        /// How strongly what a cast lays lands — the multiplier the values of its buff are read through.
        /// Deliberately one knob rather than one per value: an ability that scales its buff scales all
        /// of it, or the key would say nothing about what an augment standing on it actually buys.
        /// </summary>
        public const string Effectiveness = nameof(Effectiveness);

        /// <summary>
        /// How many attacks one cast delivers. Registered by the abilities that carry a single count;
        /// an ability whose series is a RANGE (a floor and a ceiling rolled between) does not own the
        /// concept — one number cannot say which end of that range an augment is buying, and answering
        /// it by moving both silently would make "+1 attack" mean two different things on two abilities.
        /// </summary>
        public const string Attacks = nameof(Attacks);

        /// <summary>
        /// How long the stun a cast puts on its target holds, in turns. Four abilities stun and all four
        /// mean the same turns by it, so the record that lengthens a stun is written once.
        /// </summary>
        public const string StunDuration = nameof(StunDuration);

        /// <summary>
        /// How long a poison stack laid by a cast holds, in turns. Owned by the abilities that poison
        /// and LENT to the ones that learn to (<c>AugmentPoisonOnHit</c>), which is what lets an augment
        /// that lengthens poison work on an ability that never knew the word until another augment
        /// taught it: the applier registers the key, the amplifier decorates it, and neither was written
        /// for the other. An ability naming a poison duration of its own keeps its own base value.
        /// </summary>
        public const string PoisonDuration = nameof(PoisonDuration);

        /// <summary>
        /// The multiplier this cast's own damage is read through — the ability's damage as a factor
        /// rather than as a figure. Abilities disagree about the base they start from (a series that
        /// already strikes for more than one, an explosion that starts at nothing), and that is a base
        /// value and no part of the concept: an augment adding a quarter adds a quarter wherever it sits.
        /// </summary>
        public const string DamageMultiplier = nameof(DamageMultiplier);

        /// <summary>
        /// How many times a cast may be spent before it has to be recharged. One number for the whole
        /// book so that "one more charge" is a single record rather than one per charged ability.
        /// </summary>
        public const string Charges = nameof(Charges);

        /// <summary>
        /// How much a shield granted by a cast holds. The shield is the separate defence layer and NOT
        /// the barrier — the two are different pools with different rules, so an augment thickening one
        /// has no business reaching the other, and a cast that grants a barrier names that number itself.
        /// </summary>
        public const string ShieldStrength = nameof(ShieldStrength);

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

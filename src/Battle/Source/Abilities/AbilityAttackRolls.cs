namespace Battle.Source.Abilities
{
    using Core;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Data;
    using Core.Entity;
    using Core.Enums;

    /// <summary>
    /// What an ABILITY contributes to the rolls of a touch it deals. The attack pipeline rolls on the
    /// numbers the context carries, and a context left with the attacker's bare figures cannot be moved
    /// by an augment seated on the ability — so every attacking ability stamps its own bonuses here
    /// instead of copying the attacker's parameters into the initializer.
    /// <para>Two shapes of delivery, one reading of the keys: an attack carries a context to stamp, a
    /// direct hit carries none and asks for the roll and the multiplier instead. Both are here so the
    /// crit of the whole book is defined once.</para>
    /// </summary>
    public static class AbilityAttackRolls
    {
        /// <summary>Whether a DIRECT touch of the ability crits: the attacker's chance raised by a
        /// fraction of itself, for deliveries with no attack context of their own.</summary>
        public static bool RollsCritical(this IAbility ability, IFightable owner) =>
            ChanceRoll.Roll(owner.Parameters.CriticalChance * (1 + ability.ValueOr(AbilityParameter.CriticalChanceBonus, 0f)),
                CombatRandom.Rolls, owner.Parameters.GetChanceLuck(EntityParameter.CriticalChance));

        /// <summary>What a crit of the ability multiplies a direct touch by: additive by design, so a
        /// bonus only ever adds to the attacker's own multiplier.</summary>
        public static float CriticalMultiplierOf(this IAbility ability, IFightable owner) =>
            owner.Parameters.CriticalDamage + ability.ValueOr(AbilityParameter.CriticalDamageBonus, 0f);

        /// <summary>The attacker's crit raised by what the ability declares: the chance by a fraction of
        /// itself, the damage additively — one reading, the same as the multicast family's own roll.</summary>
        public static void UseCriticalOf(this IAttackContext context, IAbility ability)
        {
            context.RawCriticalChance = context.Attacker.Parameters.CriticalChance
                                        * (1 + ability.ValueOr(AbilityParameter.CriticalChanceBonus, 0f));
            context.RawCriticalDamage = context.Attacker.Parameters.CriticalDamage
                                        + ability.ValueOr(AbilityParameter.CriticalDamageBonus, 0f);
        }

        /// <summary>The attacker's accuracy raised by a fraction of itself, for the attacks of an ability
        /// that owns the concept.</summary>
        public static void UseAccuracyOf(this IAttackContext context, IAbility ability) =>
            context.RawAccuracy = context.Attacker.Parameters.Accuracy
                                  * (1 + ability.ValueOr(AbilityParameter.AccuracyBonus, 0f));
    }
}

namespace Battle.Source.Abilities
{
    using Core.Battle;
    using Core.Battle.Abilities;

    /// <summary>
    /// What an ABILITY contributes to the rolls of an attack it makes. The attack pipeline rolls on the
    /// numbers the context carries, and a context left with the attacker's bare figures cannot be moved
    /// by an augment seated on the ability — so every attacking ability stamps its own bonuses here
    /// instead of copying the attacker's parameters into the initializer.
    /// </summary>
    public static class AbilityAttackRolls
    {
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

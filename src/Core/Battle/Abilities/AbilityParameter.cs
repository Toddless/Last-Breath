namespace Core.Battle.Abilities
{
    /// <summary>
    /// Keys of the parameters every ability owns. Parameter keys are strings — the same names the
    /// JSON data and the description placeholders use; ability-specific keys live in a nested
    /// <c>Parameters</c> constants class of the ability itself.
    /// </summary>
    public static class AbilityParameter
    {
        public const string Cooldown = nameof(Cooldown);
        public const string CostType = nameof(CostType);
        public const string CostValue = nameof(CostValue);
        public const string Damage = nameof(Damage);
        public const string SpellDamageScale = nameof(SpellDamageScale);
        public const string WeaponDamageScale = nameof(WeaponDamageScale);

        /// <summary>Fractional increase of the owner's critical chance for this ability: final = owner * (1 + bonus).</summary>
        public const string CriticalChanceBonus = nameof(CriticalChanceBonus);

        /// <summary>Fractional increase of the owner's critical damage for this ability: final = owner and bonus.</summary>
        public const string CriticalDamageBonus = nameof(CriticalDamageBonus);
    }
}

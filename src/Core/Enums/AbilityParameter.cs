namespace Core.Enums
{
    public enum AbilityParameter
    {
        Cooldown,
        CostType,
        CostValue,
        Damage,
        SpellDamageScale,
        WeaponDamageScale,
        /// <summary>Fractional increase of the owner's critical chance for this ability: final = owner * (1 + bonus).</summary>
        CriticalChanceBonus,
        /// <summary>Fractional increase of the owner's critical damage for this ability: final = owner and bonus.</summary>
        CriticalDamageBonus
    }
}

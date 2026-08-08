namespace Core.Enums
{
    public enum EntityParameter
    {
        PhysicalDamage = 1,
        Intelligence,
        Dexterity,
        Strength,
        BlockChance,
        CriticalChance,
        AdditionalHitChance,
        MulticastChance,
        CriticalDamage,
        ArmorPenetration,
        Accuracy,
        SpellDamage,
        Armor,
        Evade,
        Barrier,
        Mana,
        ManaRecovery,
        MoveSpeed,
        Suppress,
        SuppressChance,
        Health,
        HealthRecovery,
        CriticalDamageMitigation,
        FireResistance,
        ColdResistance,
        LightningResistance,
        FireDamage,
        ColdDamage,
        LightningDamage,

        /// <summary>Fraction (0..1) of the target's matching resistance the owner's damage ignores —
        /// the elemental mirror of <see cref="ArmorPenetration"/>.</summary>
        FireResistancePenetration,
        ColdResistancePenetration,
        LightningResistancePenetration,

        // Aggregate ("all X") parameters: bucket-only members standing for a whole family. Never read as a
        // value — ParameterModifiersComponent folds their modifiers into each family member and fans change
        // events out to the members (see AggregateParameters for the membership map).
        AllResistance,
        AllAttribute,
        AllDefence,
        AllResistancePenetration,

        AllElementalDamage,
        /// <summary>
        /// All damage at once: phys, elemental, spell
        /// </summary>
        Damage
    }
}

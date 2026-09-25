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

        /// <summary>Flat barrier the owner regains at the start of every turn of his, capped by <see cref="Barrier"/>.</summary>
        BarrierRecovery,
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
        PoisonResistance,
        FireDamage,
        ColdDamage,
        LightningDamage,
        PoisonDamageMultiplier,
        BurningDamageMultiplier,
        BleedDamageMultiplier,

        /// <summary>Fraction (0..1) of the target's matching resistance the owner's damage ignores —
        /// the elemental mirror of <see cref="ArmorPenetration"/>.</summary>
        FireResistancePenetration,
        ColdResistancePenetration,
        LightningResistancePenetration,
        PoisonResistancePenetration,

        // Aggregate ("all X") parameters: bucket-only members standing for a whole family. Never read as a
        // value — ParameterModifiersComponent folds their modifiers into each family member and fans change
        // events out to the members (see AggregateParameters for the membership map).
        AllResistance,
        AllAttribute,
        AllDefence,
        AllResistancePenetration,
        AllDoTDamageMultiplier,

        /// <summary>
        /// All elemental damage: cold, light, fire
        /// </summary>
        AllElementalDamage,

        /// <summary>
        /// All damage at once: phys, elemental, spell
        /// </summary>
        Damage,

        // New members go HERE, at the end: a save writes an entity parameter as its ordinal, so a member
        // inserted above silently remaps every parameter under it in files already on disk.

        /// <summary>Ceiling the matching resistance mitigates at (0.75 = 75%). The resistance total itself is
        /// not capped: what stands above the maximum is a reserve against resistance shred.</summary>
        FireResistanceMaximum,
        ColdResistanceMaximum,
        LightningResistanceMaximum,
        PoisonResistanceMaximum,

        /// <summary>Aggregate of the four maximums (see the bucket block above).</summary>
        AllResistanceMaximum
    }
}

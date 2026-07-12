namespace Core.Enums
{
    public enum EntityParameter
    {
        Damage = 1,
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
        Health,
        HealthRecovery,
        CriticalDamageMitigation,
        FireResistance,
        ColdResistance,
        LightningResistance,

        // Aggregate ("all X") parameters: bucket-only members standing for a whole family. Never read as a
        // value — ParameterModifiersComponent folds their modifiers into each family member and fans change
        // events out to the members (see AggregateParameters for the membership map).
        AllResistance,
        AllAttribute,
        AllDefence,
    }
}

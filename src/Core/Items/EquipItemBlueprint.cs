namespace Core.Items
{
    using System.Collections.Generic;
    using Data.EquipData;
    using Enums;
    using Modifiers;

    /// <summary>Weapon-only block of a blueprint — the base combat stats the factory needs to news up
    /// a <see cref="WeaponItem"/>.</summary>
    public sealed record WeaponBlueprint(
        WeaponType WeaponType,
        Handedness Handedness,
        float Damage,
        float CriticalChance,
        float CriticalDamage);

    /// <summary>Parsed grant spec. Modifier lines are shared templates: the factory mints fresh
    /// instances per grant, so one blueprint safely feeds any number of items.</summary>
    public sealed record GrantBlueprint(
        GrantKind Kind,
        string Id,
        List<IModifier> Modifiers,
        IReadOnlyDictionary<string, float> Properties);

    /// <summary>The unmaterialized recipe of one equip item: authored lines stay descriptors (value
    /// ranges unrolled) until <see cref="IEquipItemMinter"/> mints a concrete instance. Blueprints are
    /// the ONLY representation of equip templates — they never enter the item dictionary, so no code
    /// path can hand out a shared "template item" by mistake.</summary>
    public sealed record EquipItemBlueprint
    {
        public required string Id { get; init; }
        public EquipmentPiece Piece { get; init; }

        /// <summary>Present only when <see cref="Piece"/> is <see cref="EquipmentPiece.Weapon"/>.</summary>
        public WeaponBlueprint? Weapon { get; init; }

        public Rarity Rarity { get; init; }
        public string[] Tags { get; init; } = [];

        /// <summary>Starting level spec: fixed (Min == Max) or a range the minter rolls.</summary>
        public LevelRangeData UpdateLevel { get; init; }

        public int MaxUpdateLevel { get; init; }
        public IReadOnlyList<IModifierDescriptor> Implicits { get; init; } = [];
        public IReadOnlyList<IModifierDescriptor> Modifiers { get; init; } = [];
        public IReadOnlyList<GrantBlueprint> Grants { get; init; } = [];
    }
}

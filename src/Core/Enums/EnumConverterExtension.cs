namespace Core.Enums
{
    using System;
    using Godot;

    public static class EnumConverterExtension
    {
        /// <summary>Statuses that make the fighter skip the action phase of the turn.</summary>
        private const StatusEffects SkipTurnStatuses = StatusEffects.Stun | StatusEffects.Freeze;

        /// <summary>Which of the fighter's statuses (if any) forces the turn to be skipped.</summary>
        public static StatusEffects GetSkipTurnCause(this StatusEffects effects) => effects & SkipTurnStatuses;

        public static EquipmentCategory ConvertEquipmentPartToCategory(this EquipmentPiece equipment) => equipment switch
        {
            EquipmentPiece.Body => EquipmentCategory.Armor,
            EquipmentPiece.Cloak => EquipmentCategory.Armor,
            EquipmentPiece.Gloves => EquipmentCategory.Armor,
            EquipmentPiece.Boots => EquipmentCategory.Armor,
            EquipmentPiece.Helmet => EquipmentCategory.Armor,
            EquipmentPiece.Amulet or EquipmentPiece.Belt or EquipmentPiece.Ring or EquipmentPiece.Ring2 => EquipmentCategory.Jewellery,
            EquipmentPiece.Weapon => EquipmentCategory.Weapon,
            _ => throw new ArgumentOutOfRangeException(nameof(equipment))
        };

        /// <summary>The paperdoll has two ring SLOTS while ring items all carry the single Ring
        /// piece: a slot key maps back to the item piece it accepts.</summary>
        public static EquipmentPiece AcceptedItemPiece(this EquipmentPiece slot) =>
            slot == EquipmentPiece.Ring2 ? EquipmentPiece.Ring : slot;

        /// <summary>Creation-rune floor: lower enum value = better, so the roll is raised to the floor
        /// (Math.Min over the enum value) when worse and left untouched when already better.</summary>
        public static Rarity ApplyRarityFloor(this Rarity rolled, Rarity? minRarity) =>
            minRarity == null ? rolled : (Rarity)Math.Min((byte)rolled, (byte)minRarity.Value);

        public static DamageType GetDamageType(this StatusEffects effect) => effect switch
        {
            StatusEffects.Bleed => DamageType.Bleed,
            StatusEffects.Burning => DamageType.Burning,
            StatusEffects.Poison => DamageType.Poison,
            _ => DamageType.Pure
        };

        public static float ConvertEntityTypeToThresholdPenalty(this EntityType type) => type switch
        {
            EntityType.Regular => 0f,
            EntityType.Special => 0.05f,
            EntityType.Elit => 0.1f,
            EntityType.Unique => 0.15f,
            EntityType.Boss => 0.20f,
            EntityType.Archon => 0.25f,
            _ => 0f
        };

        public static Key GetKeyAssociatedWithNumber(this int number) => number switch
        {
            1 => Key.Key1,
            2 => Key.Key2,
            3 => Key.Key3,
            4 => Key.Key4,
            5 => Key.Key5,
            6 => Key.Key6,
            7 => Key.Key7,
            8 => Key.Key8,
            9 => Key.Key9,
            10 => Key.Key0,
            _ => throw new ArgumentOutOfRangeException(nameof(number), number, null)
        };
    }
}

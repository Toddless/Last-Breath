namespace Core.Enums
{
    using System;
    using Godot;

    public static class EnumConverterExtension
    {
        public static EquipmentCategory ConvertEquipmentPartToCategory(this EquipmentPiece equipment) => equipment switch
        {
            EquipmentPiece.Body => EquipmentCategory.Armor,
            EquipmentPiece.Cloak => EquipmentCategory.Armor,
            EquipmentPiece.Gloves => EquipmentCategory.Armor,
            EquipmentPiece.Boots => EquipmentCategory.Armor,
            EquipmentPiece.Helmet => EquipmentCategory.Armor,
            EquipmentPiece.Amulet or EquipmentPiece.Belt or EquipmentPiece.Ring => EquipmentCategory.Jewellery,
            EquipmentPiece.Weapon => EquipmentCategory.Weapon,
            _ => throw new ArgumentOutOfRangeException(nameof(equipment))
        };

        public static DamageType GetDamageType(this StatusEffects effect) => effect switch
        {
            StatusEffects.Bleed => DamageType.Bleed,
            StatusEffects.Burning => DamageType.Burning,
            StatusEffects.Poison => DamageType.Poison,
            _ => DamageType.Pure
        };

        public static int ConvertRarityToItemModifierAmount(this Rarity rarity) => rarity switch
        {
            Rarity.Uncommon => 1,
            Rarity.Rare => 2,
            Rarity.Epic => 3,
            Rarity.Legendary => 4,
            _ => 0
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

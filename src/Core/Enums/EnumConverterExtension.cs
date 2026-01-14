namespace Core.Enums
{
    using System;
    using Godot;

    public static class EnumConverterExtension
    {
        public static EquipmentCategory ConvertEquipmentPartToCategory(this EquipmentType equipment)
        {
            return equipment switch
            {
                EquipmentType.Body => EquipmentCategory.Armor,
                EquipmentType.Cloak => EquipmentCategory.Armor,
                EquipmentType.Gloves => EquipmentCategory.Armor,
                EquipmentType.Boots => EquipmentCategory.Armor,
                EquipmentType.Helmet => EquipmentCategory.Armor,
                EquipmentType.Amulet or EquipmentType.Belt or EquipmentType.Ring => EquipmentCategory.Jewellery,
                EquipmentType.Weapon => EquipmentCategory.Weapon,
                _ => throw new ArgumentOutOfRangeException(nameof(equipment))
            };
        }

        public static DamageType GetDamageType(this StatusEffects effect)
        {
            return effect switch
            {
                StatusEffects.Bleed => DamageType.Bleed,
                StatusEffects.Burning => DamageType.Burning,
                StatusEffects.Poison => DamageType.Poison,
                _ => DamageType.Normal
            };
        }

        public static Key GetKeyAssociatedWithNumber(this int number)
        {
            return number switch
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
}

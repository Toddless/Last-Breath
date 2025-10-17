namespace Core.Enums
{
    public static class EnumConverterExtension
    {
        public static EquipmentCategory ConvertEquipmentPartToCategory(this EquipmentType equipment)
        {
            return equipment switch
            {
                EquipmentType.BodyArmor => EquipmentCategory.Armor,
                EquipmentType.Cloak => EquipmentCategory.Armor,
                EquipmentType.Gloves => EquipmentCategory.Armor,
                EquipmentType.Boots => EquipmentCategory.Armor,
                EquipmentType.Helmet => EquipmentCategory.Armor,
                EquipmentType.Amulet => EquipmentCategory.Jewellery,
                EquipmentType.Belt => EquipmentCategory.Jewellery,
                EquipmentType.Ring => EquipmentCategory.Jewellery,
                EquipmentType.Weapon => EquipmentCategory.Weapon,
                _ => throw new System.ArgumentOutOfRangeException(nameof(equipment))
            };
        }
    }
}

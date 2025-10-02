namespace Core.Enums
{
    public static class EnumConverterExtension
    {
        public static string ConvertEquipmentPartToCategory(this EquipmentType equipment)
        {
            return equipment switch
            {
                EquipmentType.BodyArmor => "Armor",
                EquipmentType.Cloak => "Armor",
                EquipmentType.Gloves => "Armor",
                EquipmentType.Boots => "Armor",
                EquipmentType.Helmet => "Armor",
                EquipmentType.Amulet => "Jewellery",
                EquipmentType.Belt => "Jewellery",
                EquipmentType.Ring => "Jewellery",
                EquipmentType.Weapon => "Weapon",
                _ => string.Empty,
            };
        }
    }
}

namespace LastBreath.Script.Items.ItemData
{
    using LastBreath.Script.Enums;

    public interface IItemStatsHandler
    {
        public void LoadData();
        public ItemStats GetAttributeJewelleryStats(JewelleryType type, GlobalRarity rarity, AttributeType attributeType);
        public ItemStats GetAttributeBodyArmorStats(BodyArmorType type, GlobalRarity rarity, AttributeType attributeType);
        public ItemStats GetJewelleryStats(JewelleryType type, GlobalRarity rarity);
        public ItemStats GetBodyArmorStats(BodyArmorType type, GlobalRarity rarity);
        public ItemStats GetWeaponStats(WeaponType type, GlobalRarity rarity);
    }
}

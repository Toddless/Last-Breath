namespace LastBreath.Script.Items.ItemData
{
    using Contracts.Enums;

    public interface IItemsMediaHandler
    {
        public void LoadData();
        public ItemMediaData GetWeaponMediaData(WeaponType type, GlobalRarity rarity);
        public ItemMediaData GetAttributeJewelleryMediaData(JewelleryType type, GlobalRarity rarity, AttributeType attributeType);
        public ItemMediaData GetAttributeBodyArmorMediaData(BodyArmorType type, GlobalRarity rarity, AttributeType attributeType);
        public ItemMediaData GetJewelleryMediaData(JewelleryType type, GlobalRarity rarity);
        public ItemMediaData GetBodyArmorMediaData(BodyArmorType type, GlobalRarity rarity);
    }
}

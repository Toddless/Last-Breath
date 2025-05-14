namespace Playground.Script.Items.ItemData
{
    using Playground.Script.Enums;

    public interface IItemsMediaHandler
    {
        public void LoadData();
        public ItemMediaData GetWeaponMediaData(WeaponType type, GlobalRarity rarity);
        public ItemMediaData GetAttributeJewelleryMediaData(JewelleryType type, GlobalRarity rarity, AttributeType attributeType);
        public ItemMediaData GetAttributeArmorMediaData(BodyArmorType type, GlobalRarity rarity, AttributeType attributeType);
        public ItemMediaData GetJewelleryMediaData(JewelleryType type, GlobalRarity rarity);
        public ItemMediaData GetArmorMediaData(BodyArmorType type, GlobalRarity rarity);
    }
}

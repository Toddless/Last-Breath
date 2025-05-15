namespace Playground.Script.Items
{
    using Playground.Script.Enums;
    using Playground.Script.Items.ItemData;

    public partial class BaseAttributeEquipItem : EquipItem
    {
        protected AttributeType Attribute;

        public BaseAttributeEquipItem(GlobalRarity rarity, AttributeType attributeType, EquipmentPart equipmentPart)
            : base(rarity, equipmentPart)
        {
            Attribute = attributeType;
            LoadData();
        }

        protected override ItemStats? GetItemStats() => EquipmentPart switch
        {
            EquipmentPart.BodyArmor => DiContainer.GetService<IItemStatsHandler>()?.GetAttributeBodyArmorStats(BodyArmorType.BodyArmor, Rarity, Attribute),
            EquipmentPart.Gloves => DiContainer.GetService<IItemStatsHandler>()?.GetAttributeBodyArmorStats(BodyArmorType.Gloves, Rarity, Attribute),
            EquipmentPart.Boots => DiContainer.GetService<IItemStatsHandler>()?.GetAttributeBodyArmorStats(BodyArmorType.Boots, Rarity, Attribute),
            EquipmentPart.Helmet => DiContainer.GetService<IItemStatsHandler>()?.GetAttributeBodyArmorStats(BodyArmorType.Helmet, Rarity, Attribute),
            EquipmentPart.Ring => DiContainer.GetService<IItemStatsHandler>()?.GetAttributeJewelleryStats(JewelleryType.Ring, Rarity, Attribute),
            _ => null
        };

        protected override ItemMediaData? GetItemMediaData() => EquipmentPart switch
        {
            EquipmentPart.BodyArmor => ItemsMediaHandler.Inctance?.GetAttributeBodyArmorMediaData(BodyArmorType.BodyArmor, Rarity, Attribute),
            EquipmentPart.Gloves => ItemsMediaHandler.Inctance?.GetAttributeBodyArmorMediaData(BodyArmorType.Gloves, Rarity, Attribute),
            EquipmentPart.Boots => ItemsMediaHandler.Inctance?.GetAttributeBodyArmorMediaData(BodyArmorType.Boots, Rarity, Attribute),
            EquipmentPart.Helmet => ItemsMediaHandler.Inctance?.GetAttributeBodyArmorMediaData(BodyArmorType.Helmet, Rarity, Attribute),
            EquipmentPart.Ring => ItemsMediaHandler.Inctance?.GetAttributeJewelleryMediaData(JewelleryType.Ring, Rarity, Attribute),
            _ => null
        };
    }
}

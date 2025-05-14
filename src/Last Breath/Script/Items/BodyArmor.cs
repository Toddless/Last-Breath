namespace Playground.Script.Items
{
    using Playground.Script.Enums;
    using Playground.Script.Items.ItemData;

    public partial class BodyArmor : EquipItem
    {
        protected AttributeType Attribute;

        public BodyArmor(GlobalRarity rarity, AttributeType attributeType)
        {
            Rarity = rarity;
            EquipmentPart = EquipmentPart.BodyArmor;
            Attribute = attributeType;
            LoadData();
        }

        protected override void LoadData()
        {
            var itemStats = DiContainer.GetService<IItemStatsHandler>()?.GetAttributeBodyArmorStats(BodyArmorType.BodyArmor, Rarity, Attribute);
            if (itemStats == null)
            {
                //TODO Log
                return;
            }
            BaseModifiers = ModifiersCreator.ItemStatsToModifier(itemStats, this);

            var itemMediaData = ItemsMediaHandler.Inctance?.GetAttributeArmorMediaData(BodyArmorType.BodyArmor, Rarity, Attribute);

            if (itemMediaData == null)
            {
                //TODO Log
                return;
            }
            Icon = itemMediaData.IconTexture;
            Description = itemMediaData.Description;
            ItemName = itemMediaData.Name;
            FullImage = itemMediaData.FullTexture;
        }
    }
}

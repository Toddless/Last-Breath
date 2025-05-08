namespace Playground.Script.Items
{
    using Playground.Script.Enums;

    public partial class BodyArmor : EquipItem
    {
        protected AttributeType Attribute;

        public BodyArmor(GlobalRarity rarity, AttributeType attributeType)
        {
            EquipmentPart = EquipmentPart.BodyArmor;
            this.Attribute = attributeType;
            LoadData(rarity);
        }

        protected override void LoadData(GlobalRarity rarity)
        {
            // var data = ItemDataHandler.GetArmorStats(rarity, Attribute, BodyArmorType.BodyArmor);
        }
    }
}

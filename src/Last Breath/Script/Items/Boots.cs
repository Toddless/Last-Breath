namespace Playground.Script.Items
{
    using Playground.Script.Enums;

    public partial class Boots : BaseAttributeEquipItem
    {
        public Boots(GlobalRarity rarity, AttributeType attributeType)
            : base(rarity, attributeType, EquipmentPart.Boots)
        {
        }
    }
}

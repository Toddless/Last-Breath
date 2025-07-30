namespace LastBreath.Script.Items
{
    using LastBreath.Script.Enums;

    public partial class Boots : BaseAttributeEquipItem
    {
        public Boots(GlobalRarity rarity, AttributeType attributeType)
            : base(rarity, attributeType, EquipmentPart.Boots)
        {
        }
    }
}

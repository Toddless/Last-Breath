namespace LastBreath.Script.Items
{
    using LastBreath.Script.Enums;

    public partial class BodyArmor : BaseAttributeEquipItem
    {
        public BodyArmor(GlobalRarity rarity, AttributeType attributeType)
            : base(rarity, attributeType, EquipmentPart.BodyArmor)
        {
        }
    }
}

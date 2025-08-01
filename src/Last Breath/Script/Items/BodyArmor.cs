namespace LastBreath.Script.Items
{
    using Contracts.Enums;

    public partial class BodyArmor : BaseAttributeEquipItem
    {
        public BodyArmor(GlobalRarity rarity, AttributeType attributeType)
            : base(rarity, attributeType, EquipmentPart.BodyArmor)
        {
        }
    }
}

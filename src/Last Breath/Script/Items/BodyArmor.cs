namespace LastBreath.Script.Items
{
    using Core.Enums;

    public partial class BodyArmor : EquipItem
    {
        public BodyArmor(Rarity rarity, AttributeType attributeType)
            : base(rarity, EquipmentPart.BodyArmor, attributeType)
        {
        }
    }
}

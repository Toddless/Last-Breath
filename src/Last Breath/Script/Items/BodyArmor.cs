namespace LastBreath.Script.Items
{
    using Contracts.Enums;

    public partial class BodyArmor : EquipItem
    {
        public BodyArmor(Rarity rarity, AttributeType attributeType)
            : base(rarity, EquipmentPart.BodyArmor, attributeType)
        {
            LoadData();
        }
    }
}

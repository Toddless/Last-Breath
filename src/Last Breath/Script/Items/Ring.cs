namespace LastBreath.Script.Items
{
    using LastBreath.Script.Enums;

    public partial class Ring : BaseAttributeEquipItem
    {
        public Ring(GlobalRarity rarity, AttributeType attributeType)
            : base(rarity, attributeType, equipmentPart: EquipmentPart.Ring)
        {
        }
    }
}

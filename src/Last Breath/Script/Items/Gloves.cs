namespace LastBreath.Script.Items
{
    using LastBreath.Script.Enums;

    public partial class Gloves : BaseAttributeEquipItem
    {
        public Gloves(GlobalRarity rarity, AttributeType attributeType)
            : base(rarity, attributeType, equipmentPart: EquipmentPart.Gloves)
        {
        }
    }
}

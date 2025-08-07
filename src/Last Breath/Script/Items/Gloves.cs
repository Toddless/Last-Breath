namespace LastBreath.Script.Items
{
    using Core.Enums;

    public partial class Gloves : EquipItem
    {
        public Gloves(Rarity rarity, AttributeType attributeType)
            : base(rarity, equipmentPart: EquipmentPart.Gloves, attributeType)
        {
        }
    }
}

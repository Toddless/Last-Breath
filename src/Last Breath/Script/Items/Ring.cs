namespace LastBreath.Script.Items
{
    using Contracts.Enums;

    public partial class Ring : BaseAttributeEquipItem
    {
        public Ring(GlobalRarity rarity, AttributeType attributeType)
            : base(rarity, attributeType, equipmentPart: EquipmentPart.Ring)
        {
        }
    }
}

namespace LastBreath.Script.Items
{
    using Contracts.Enums;

    public partial class Helmet : BaseAttributeEquipItem
    {
        public Helmet(GlobalRarity rarity, AttributeType attributeType)
            : base(rarity, attributeType, equipmentPart: EquipmentPart.Helmet)
        {
        }
    }
}

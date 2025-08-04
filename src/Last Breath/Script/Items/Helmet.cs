namespace LastBreath.Script.Items
{
    using Contracts.Enums;

    public partial class Helmet : EquipItem
    {
        public Helmet(Rarity rarity, AttributeType attributeType)
            : base(rarity, equipmentPart: EquipmentPart.Helmet, attributeType)
        {
            LoadData();
        }
    }
}

namespace LastBreath.Script.Items
{
    using Core.Enums;

    public partial class Ring : EquipItem
    {
        public Ring(Rarity rarity, AttributeType attributeType)
            : base(rarity, equipmentPart: EquipmentPart.Ring, attributeType)
        {
            LoadData();
        }
    }
}

namespace LastBreath.Script.Items
{
    using Contracts.Enums;

    public partial class Boots : EquipItem
    {
        public Boots(Rarity rarity, AttributeType attributeType)
            : base(rarity, EquipmentPart.Boots, attributeType)
        {
            LoadData();
        }
    }
}

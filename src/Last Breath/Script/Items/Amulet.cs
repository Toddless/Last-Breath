namespace LastBreath.Script.Items
{
    using Core.Enums;

    public partial class Amulet : EquipItem
    {
        public Amulet(Rarity rarity)
            : base(rarity, equipmentPart: EquipmentPart.Amulet, type: AttributeType.None)
        {
            LoadData();
        }
    }
}

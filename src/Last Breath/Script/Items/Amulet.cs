namespace LastBreath.Script.Items
{
    using LastBreath.Script.Enums;

    public partial class Amulet : EquipItem
    {
        public Amulet(GlobalRarity rarity)
            : base(rarity, equipmentPart: EquipmentPart.Amulet)
        {
        }
    }
}

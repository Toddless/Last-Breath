namespace Playground.Script.Items
{
    using Playground.Script.Enums;

    public partial class Amulet : EquipItem
    {
        public Amulet(GlobalRarity rarity)
            : base(rarity, equipmentPart: EquipmentPart.Amulet)
        {
        }
    }
}

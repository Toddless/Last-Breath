namespace Playground.Script.Items
{
    using Playground.Script.Enums;

    public partial class Belt : EquipItem
    {
        public Belt(GlobalRarity rarity)
            : base(rarity, equipmentPart: EquipmentPart.Belt)
        {
        }
    }
}

namespace LastBreath.Script.Items
{
    using Contracts.Enums;

    public partial class Belt : EquipItem
    {
        public Belt(GlobalRarity rarity)
            : base(rarity, equipmentPart: EquipmentPart.Belt)
        {
        }
    }
}

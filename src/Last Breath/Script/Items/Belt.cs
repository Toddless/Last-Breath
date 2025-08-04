namespace LastBreath.Script.Items
{
    using Contracts.Enums;

    public partial class Belt : EquipItem
    {
        public Belt(Rarity rarity)
            : base(rarity, equipmentPart: EquipmentPart.Belt, type: AttributeType.None)
        {
            LoadData();
        }
    }
}

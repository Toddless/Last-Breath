namespace LastBreath.Script.Items
{
    using Contracts.Enums;

    public partial class Cloak : EquipItem
    {
        public Cloak(Rarity rarity)
            : base(rarity, EquipmentPart.Cloak, type: AttributeType.None)
        {
            LoadData();
        }
    }
}

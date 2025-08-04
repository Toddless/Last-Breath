namespace LastBreath.Script.Items
{
    using Contracts.Enums;

    public partial class Bow : WeaponItem
    {
        public Bow(Rarity rarity) : base(rarity, WeaponType.Bow)
        {
            LoadData();
        }
    }
}

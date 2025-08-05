namespace LastBreath.Script.Items
{
    using Core.Enums;

    public partial class Bow : WeaponItem
    {
        public Bow(Rarity rarity) : base(rarity, WeaponType.Bow)
        {
            LoadData();
        }
    }
}

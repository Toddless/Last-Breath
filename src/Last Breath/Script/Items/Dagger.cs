namespace LastBreath.Script.Items
{
    using Contracts.Enums;

    public partial class Dagger : WeaponItem
    {
        public Dagger(Rarity rarity) : base(rarity, WeaponType.Dagger)
        {
            LoadData();
        }
    }
}

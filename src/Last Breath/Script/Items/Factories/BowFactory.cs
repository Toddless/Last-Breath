namespace LastBreath.Script.Items.Factories
{
    using System;
    using Godot;
    using LastBreath.Script.Enums;
    using LastBreath.Script.Items;

    public class BowFactory : ItemCreator
    {
        public BowFactory(RandomNumberGenerator random)
        {
            ArgumentNullException.ThrowIfNull(random);
            Rnd = random;
        }

        public override EquipItem GenerateItem(GlobalRarity globalRarity)
        {
            return globalRarity switch
            {
                GlobalRarity.Rare => new Bow(GlobalRarity.Rare),
                GlobalRarity.Epic => new Bow(GlobalRarity.Epic),
                GlobalRarity.Legendary => new Bow(GlobalRarity.Legendary),
                _ => new Bow(GlobalRarity.Uncommon),
            };
        }
    }
}

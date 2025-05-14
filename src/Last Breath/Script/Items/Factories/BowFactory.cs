namespace Playground.Script.Items.Factories
{
    using System;
    using Godot;
    using Playground.Script.Enums;

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

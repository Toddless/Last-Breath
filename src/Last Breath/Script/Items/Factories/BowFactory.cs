namespace LastBreath.Script.Items.Factories
{
    using System;
    using Core.Enums;
    using Godot;
    using LastBreath.Script.Items;

    public class BowFactory : ItemCreator
    {
        public BowFactory(RandomNumberGenerator random)
        {
            ArgumentNullException.ThrowIfNull(random);
            Rnd = random;
        }

        public override EquipItem GenerateItem(Rarity globalRarity)
        {
            return globalRarity switch
            {
                Rarity.Rare => new Bow(Rarity.Rare),
                Rarity.Epic => new Bow(Rarity.Epic),
                Rarity.Legendary => new Bow(Rarity.Legendary),
                _ => new Bow(Rarity.Uncommon),
            };
        }
    }
}

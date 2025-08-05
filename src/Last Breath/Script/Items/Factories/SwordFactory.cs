namespace LastBreath.Script.Items.Factories
{
    using System;
    using Core.Enums;
    using Godot;
    using LastBreath.Script.Items;

    public class SwordFactory : ItemCreator
    {
        public SwordFactory(RandomNumberGenerator random)
        {
            ArgumentNullException.ThrowIfNull(random);
            Rnd = random;
        }

        public override EquipItem GenerateItem(Rarity rarity)
        {
            return rarity switch
            {
                Rarity.Rare => new Sword(Rarity.Rare),
                Rarity.Epic => new Sword(Rarity.Epic),
                Rarity.Legendary => new Sword(Rarity.Legendary),
                _ => new Sword(Rarity.Uncommon),
            };
        }
    }
}

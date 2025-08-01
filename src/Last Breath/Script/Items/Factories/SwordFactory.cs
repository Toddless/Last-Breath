namespace LastBreath.Script.Items.Factories
{
    using System;
    using Contracts.Enums;
    using Godot;
    using LastBreath.Script.Items;

    public class SwordFactory : ItemCreator
    {
        public SwordFactory(RandomNumberGenerator random)
        {
            ArgumentNullException.ThrowIfNull(random);
            Rnd = random;
        }

        public override EquipItem GenerateItem(GlobalRarity rarity)
        {
            return rarity switch
            {
                GlobalRarity.Rare => new Sword(GlobalRarity.Rare),
                GlobalRarity.Epic => new Sword(GlobalRarity.Epic),
                GlobalRarity.Legendary => new Sword(GlobalRarity.Legendary),
                _ => new Sword(GlobalRarity.Uncommon),
            };
        }
    }
}

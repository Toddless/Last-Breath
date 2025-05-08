namespace Playground.Script.Items.Factories
{
    using System;
    using Godot;
    using Playground.Script.Enums;

    public class SwordFactory : ItemCreator
    {
        public SwordFactory(RandomNumberGenerator random)
        {
            ArgumentNullException.ThrowIfNull(random);
            Rnd = random;
        }

        public override EquipItem? GenerateItem(GlobalRarity rarity)
        {
            return rarity switch
            {
                //GlobalRarity.Uncommon => new Sword(StringHelper.SwordUncommon, GlobalRarity.Uncommon, Rnd!.RandfRange(75, 100), Rnd.RandfRange(170, 195), 0.08f, ResourcePath.SwordUncommon, GD.Load<Texture2D>(TexturePaths.SwordUncommon), 1, 1),
                //GlobalRarity.Rare => new Sword(StringHelper.SwordRare, GlobalRarity.Rare, Rnd!.RandfRange(170, 200), Rnd.RandfRange(250, 300), 0.08f, ResourcePath.SwordRare, GD.Load<Texture2D>(TexturePaths.SwordRare), 1, 1),
                //GlobalRarity.Epic => new Sword(StringHelper.SwordEpic, GlobalRarity.Epic, Rnd!.RandfRange(200, 250), Rnd.RandfRange(300, 380), 0.08f, ResourcePath.SwordEpic, GD.Load<Texture2D>(TexturePaths.SwordEpic), 1, 1),
                //GlobalRarity.Legendary => new Sword(StringHelper.SwordLegendary, GlobalRarity.Legendary, Rnd!.RandfRange(300, 380), Rnd.RandfRange(400, 450), 0.08f, ResourcePath.SwordLegendary, GD.Load<Texture2D>(TexturePaths.SwordLegendary), 1, 1),
                _ => null,
            };
        }
    }
}

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

        public override Bow? GenerateItem(GlobalRarity globalRarity)
        {
            return globalRarity switch
            {
                //GlobalRarity.Uncommon => new Bow(StringHelper.BowUncommon, GlobalRarity.Uncommon, Rnd!.RandfRange(60, 90), Rnd.RandfRange(130, 180), 0.05f, ResourcePath.BowUncommon, GD.Load<Texture2D>(TexturePaths.BowUncommon), 1, 1),
                //GlobalRarity.Rare => new Bow(StringHelper.BowRare, GlobalRarity.Rare, Rnd!.RandfRange(60, 90), Rnd.RandfRange(130, 180), 0.05f, ResourcePath.BowRare, GD.Load<Texture2D>(TexturePaths.BowRare), 1, 1),
                //GlobalRarity.Epic => new Bow(StringHelper.BowEpic, GlobalRarity.Epic, Rnd!.RandfRange(60, 90), Rnd.RandfRange(130, 180), 0.05f, ResourcePath.BowEpic, GD.Load<Texture2D>(TexturePaths.BowEpic), 1, 1),
                //GlobalRarity.Legendary => new Bow(StringHelper.BowLegendary, GlobalRarity.Legendary, Rnd!.RandfRange(60, 90), Rnd.RandfRange(130, 180), 0.05f, ResourcePath.BowLegendary, GD.Load<Texture2D>(TexturePaths.BowLegendary), 1, 1),
                //_ => null,
            };
        }
    }
}

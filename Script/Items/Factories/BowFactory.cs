namespace Playground.Script.Items.Factories
{
    using Godot;
    using Playground.Script.Helpers;
    using Playground.Script.LootGenerator.BasedOnRarityLootGenerator;

    public class BowFactory : ItemCreator
    {
        private static BowFactory instance;

        private BowFactory()
        {

        }

        public static BowFactory Instance
        {
            get
            {
                instance ??= new BowFactory();
                return instance;
            }
        }

        public override Bow GenerateItem(GlobalRarity globalRarity)
        {
            // потенциальный memory leak
            return globalRarity switch
            {
                GlobalRarity.Uncommon => new Bow(StringHelper.BowUncommon, GlobalRarity.Uncommon, RandomNumberGenerator.RandfRange(65, 80), RandomNumberGenerator.RandfRange(160, 220), ResourcePath.BowUncommon, GD.Load<Texture2D>("res://Assets/Weapon/Bows/BowUncommon.png"), 1, 1),
                GlobalRarity.Rare => new Bow(StringHelper.BowRare, GlobalRarity.Rare, RandomNumberGenerator.RandfRange(70, 120), RandomNumberGenerator.RandfRange(200, 280), ResourcePath.BowRare, GD.Load<Texture2D>("res://Assets/Weapon/Bows/BowRare.png"), 1, 1),
                GlobalRarity.Epic => new Bow(StringHelper.BowEpic, GlobalRarity.Epic, RandomNumberGenerator.RandfRange(130, 160), RandomNumberGenerator.RandfRange(260, 320), ResourcePath.BowEpic, GD.Load<Texture2D>("res://Assets/Weapon/Bows/BowEpic.png"), 1, 1),
                GlobalRarity.Legendary => new Bow(StringHelper.BowLegendary, GlobalRarity.Legendary, RandomNumberGenerator.RandfRange(180, 300), RandomNumberGenerator.RandfRange(450, 600), ResourcePath.BowLegendary, GD.Load<Texture2D>("res://Assets/Weapon/Bows/BowLegendary.png"), 1, 1),
                GlobalRarity.Mythic => VeryUniqBow.Instance,
                _ => null,
            };
        }
    }
}

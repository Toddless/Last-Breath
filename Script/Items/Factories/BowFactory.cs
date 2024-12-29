namespace Playground.Script.Items.Factories
{
    using Godot;
    using Playground.Script.Enums;
    using Playground.Script.Helpers;

    public class BowFactory : ItemCreator
    {
        private readonly string _uncommonBowTexturePath = "res://Assets/Weapon/Bows/BowUncommon.png";
        private readonly string _rareBowTexturePath = "res://Assets/Weapon/Bows/BowRare.png";
        private readonly string _epicBowTexturePath = "res://Assets/Weapon/Bows/BowEpic.png";
        private readonly string _legendaryBowTexturePath = "res://Assets/Weapon/Bows/BowLegendary.png";

        private static BowFactory? _instance = null;

        private BowFactory()
        {
            
        }

        public static BowFactory Instance
        {
            get
            {
                _instance ??= new BowFactory();
                return _instance;
            }
        }

        public override Bow? GenerateItem(GlobalRarity globalRarity)
        {
            return globalRarity switch
            {
                GlobalRarity.Uncommon => new Bow(StringHelper.BowUncommon, GlobalRarity.Uncommon, 90, 180, 0.05f, ResourcePath.BowUncommon, GD.Load<Texture2D>(_uncommonBowTexturePath), 1, 1),
                GlobalRarity.Rare => new Bow(StringHelper.BowRare, GlobalRarity.Rare, 90, 180, 0.05f, ResourcePath.BowRare, GD.Load<Texture2D>(_rareBowTexturePath), 1, 1),
                GlobalRarity.Epic => new Bow(StringHelper.BowEpic, GlobalRarity.Epic, 90, 180, 0.05f, ResourcePath.BowEpic, GD.Load<Texture2D>(_epicBowTexturePath), 1, 1),
                GlobalRarity.Legendary => new Bow(StringHelper.BowLegendary, GlobalRarity.Legendary, 90, 180, 0.05f, ResourcePath.BowLegendary, GD.Load<Texture2D>(_legendaryBowTexturePath), 1, 1),
                GlobalRarity.Mythic => VeryUniqBow.Instance,
                _ => null,
            };
        }
    }
}

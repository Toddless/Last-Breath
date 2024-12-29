namespace Playground.Script.Items.Factories
{
    using Godot;
    using Playground.Script.Enums;
    using Playground.Script.Helpers;

    public class SwordFactory : ItemCreator
    {
        private static SwordFactory? _instance = null;

        private SwordFactory()
        {

        }

        public static SwordFactory Instance
        {
            get
            {
                _instance ??= new SwordFactory();
                return _instance;
            }
        }

        public override Sword? GenerateItem(GlobalRarity rarity)
        {
            return rarity switch
            {
                GlobalRarity.Uncommon => new Sword(StringHelper.SwordUncommon, GlobalRarity.Uncommon, RandomNumberGenerator.RandfRange(75, 100), RandomNumberGenerator.RandfRange(170, 195), 0.08f ,ResourcePath.SwordUncommon, GD.Load<Texture2D>("res://Assets/Weapon/Swords/SwordUncommon.png"), 1, 1),
                GlobalRarity.Rare => new Sword(StringHelper.SwordRare, GlobalRarity.Rare, RandomNumberGenerator.RandfRange(170, 200), RandomNumberGenerator.RandfRange(250, 300), 0.08f, ResourcePath.SwordRare, GD.Load<Texture2D>("res://Assets/Weapon/Swords/SwordRare.png"), 1, 1),
                GlobalRarity.Epic => new Sword(StringHelper.SwordEpic, GlobalRarity.Epic, RandomNumberGenerator.RandfRange(200, 250), RandomNumberGenerator.RandfRange(300, 380), 0.08f, ResourcePath.SwordEpic, GD.Load<Texture2D>("res://Assets/Weapon/Swords/SwordEpic.png"), 1, 1),
                GlobalRarity.Legendary => new Sword(StringHelper.SwordLegendary, GlobalRarity.Legendary, RandomNumberGenerator.RandfRange(300, 380), RandomNumberGenerator.RandfRange(400, 450), 0.08f, ResourcePath.SwordLegendary, GD.Load<Texture2D>("res://Assets/Weapon/Swords/SwordLegendary.png"), 1, 1),
                GlobalRarity.Mythic => VeryUniqSword.Instance,
                _ => null,
            };
        }
    }
}

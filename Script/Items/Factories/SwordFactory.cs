namespace Playground.Script.Items.Factories
{
    using Godot;
    using Playground.Script.Helpers;
    using Playground.Script.LootGenerator.BasedOnRarityLootGenerator;

    public class SwordFactory : ItemCreator
    {
        private static SwordFactory instance = null;

        private SwordFactory()
        {

        }

        public static SwordFactory Instance
        {
            get
            {
                instance ??= new SwordFactory();
                return instance;
            }
        }

        public override Sword GenerateItem(GlobalRarity rarity)
        {
            return rarity switch
            {
                GlobalRarity.Common => new Sword(StringHelper.SwordCommon, GlobalRarity.Common, RandomNumberGenerator.RandfRange(30, 45), RandomNumberGenerator.RandfRange(140, 160), string.Empty, null, 1, 1),
                GlobalRarity.Uncommon => new Sword(StringHelper.SwordUncommon, GlobalRarity.Uncommon, RandomNumberGenerator.RandfRange(75, 100), RandomNumberGenerator.RandfRange(170, 195), ResourcePath.SwordUncommon, GD.Load<Texture2D>("res://Assets/Weapon/Swords/SwordUncommon.png"), 1, 1),
                GlobalRarity.Rare => new Sword(StringHelper.SwordRare, GlobalRarity.Rare, RandomNumberGenerator.RandfRange(170, 200), RandomNumberGenerator.RandfRange(250, 300), ResourcePath.SwordRare, GD.Load<Texture2D>("res://Assets/Weapon/Swords/SwordRare.png"), 1, 1),
                GlobalRarity.Epic => new Sword(StringHelper.SwordEpic, GlobalRarity.Epic, RandomNumberGenerator.RandfRange(200, 250), RandomNumberGenerator.RandfRange(300, 380), ResourcePath.SwordEpic, GD.Load<Texture2D>("res://Assets/Weapon/Swords/SwordEpic.png"), 1, 1),
                GlobalRarity.Legendary => new Sword(StringHelper.SwordLegendary, GlobalRarity.Legendary, RandomNumberGenerator.RandfRange(300, 380), RandomNumberGenerator.RandfRange(400, 450), ResourcePath.SwordLegendary, GD.Load<Texture2D>("res://Assets/Weapon/Swords/SwordLegendary.png"), 1, 1),
                GlobalRarity.Mythic => VeryUniqSword.Instance,
                _ => null,
            };
        }
    }
}

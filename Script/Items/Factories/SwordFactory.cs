using Godot;
using Playground.Script.LootGenerator.BasedOnRarityLootGenerator;
using System.Collections.Generic;

namespace Playground.Script.Items.Factories
{
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
                GlobalRarity.Common => new Sword("Dagger Bronze", GlobalRarity.Common, RandomNumberGenerator.RandfRange(30, 45), RandomNumberGenerator.RandfRange(140, 160), string.Empty, null, 1, 1),
                GlobalRarity.Uncommon => new Sword("Sword Kooper", GlobalRarity.Uncommon, RandomNumberGenerator.RandfRange(75, 100), RandomNumberGenerator.RandfRange(170, 195), "res://Resource/SwordUncommon.tres", GD.Load<Texture2D>("res://Assets/Weapon/Swords/SwordUncommon.png"), 1, 1),
                GlobalRarity.Rare => new Sword("Sword Silver", GlobalRarity.Rare, RandomNumberGenerator.RandfRange(170, 200), RandomNumberGenerator.RandfRange(250, 300), "res://Resource/SwordRare.tres", GD.Load<Texture2D>("res://Assets/Weapon/Swords/SwordRare.png"), 1, 1),
                GlobalRarity.Epic => new Sword("Sword Gold", GlobalRarity.Epic, RandomNumberGenerator.RandfRange(200, 250), RandomNumberGenerator.RandfRange(300, 380), "res://Resource/SwordEpic.tres", GD.Load<Texture2D>("res://Assets/Weapon/Swords/SwordEpic.png"), 1, 1),
                GlobalRarity.Legendary => new Sword("Sword Phoenix Fire", GlobalRarity.Legendary, RandomNumberGenerator.RandfRange(300, 380), RandomNumberGenerator.RandfRange(400, 450), "res://Resource/SwordLegendary.tres", GD.Load<Texture2D>("res://Assets/Weapon/Swords/SwordLegendary.png"), 1, 1),
                GlobalRarity.Mythic => VeryUniqSword.Instance,
                _ => null,
            };
        }
    }
}

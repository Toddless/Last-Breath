namespace Playground.Script.Items.Factories
{
    using Godot;
    using Playground.Script.Enums;
    using Playground.Script.Helpers;
    using Playground.Script.Items.UniqItems;

    public class BodyArmorFactory : ItemCreator
    {
        public BodyArmorFactory()
        {
            
        }

        public override BodyArmor? GenerateItem(GlobalRarity rarity)
        {
            return rarity switch
            {
                GlobalRarity.Uncommon => new BodyArmor("Iron BodyArmor", GlobalRarity.Uncommon, RandomNumberGenerator.RandfRange(160, 220), RandomNumberGenerator.RandfRange(150, 300), ResourcePath.BodyArmorUncommon, GD.Load<Texture2D>("res://Assets/BodyArmor/Uncommon.png"), 1, 1),
                GlobalRarity.Rare => new BodyArmor("Silver BodyArmor", GlobalRarity.Rare, RandomNumberGenerator.RandfRange(200, 280), RandomNumberGenerator.RandfRange(250, 450), ResourcePath.BodyArmorRare, GD.Load<Texture2D>("res://Assets/BodyArmor/Rare.png"), 1, 1),
                GlobalRarity.Epic => new("Golden BodyArmor", GlobalRarity.Epic, RandomNumberGenerator.RandfRange(260, 320), RandomNumberGenerator.RandfRange(500, 750), ResourcePath.BodyArmorEpic, GD.Load<Texture2D>("res://Assets/BodyArmor/Epic.png"), 1, 1),
                GlobalRarity.Legendary => new BodyArmor("Phoenix BodyArmor", GlobalRarity.Legendary, RandomNumberGenerator.RandfRange(450, 600), RandomNumberGenerator.RandfRange(600, 900), ResourcePath.BodyArmorLegendary, GD.Load<Texture2D>("res://Assets/BodyArmor/Legendary.png"), 1, 1),
                GlobalRarity.Mythic => VeryUniqBodyArmor.Instance,
                _=> null,
            };
        }
    }
}

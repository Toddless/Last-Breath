namespace LastBreath.Script.Items.Factories
{
    using System;
    using Godot;
    using LastBreath.Script.Enums;
    using LastBreath.Script.Items;

    public class BodyArmorFactory : ItemCreator
    {
        public BodyArmorFactory(RandomNumberGenerator random)
        {
            ArgumentNullException.ThrowIfNull(random);
            Rnd = random;
        }

        public override EquipItem? GenerateItem(GlobalRarity rarity)
        {
            return rarity switch
            {
                //GlobalRarity.Uncommon => new BodyArmor(StringHelper.BodyArmorUncommon, GlobalRarity.Uncommon, Rnd!.RandfRange(160, 220), Rnd.RandfRange(150, 300), ResourcePath.BodyArmorUncommon, GD.Load<Texture2D>(TexturePaths.BodyArmorUncommon), 1, 1),
                //GlobalRarity.Rare => new BodyArmor(StringHelper.BodyArmorRare, GlobalRarity.Rare, Rnd!.RandfRange(200, 280), Rnd.RandfRange(250, 450), ResourcePath.BodyArmorRare, GD.Load<Texture2D>(TexturePaths.BodyArmorRare), 1, 1),
                //GlobalRarity.Epic => new(StringHelper.BodyArmorEpic, GlobalRarity.Epic, Rnd!.RandfRange(260, 320), Rnd.RandfRange(500, 750), ResourcePath.BodyArmorEpic, GD.Load<Texture2D>(TexturePaths.BodyArmorEpic), 1, 1),
                //GlobalRarity.Legendary => new BodyArmor(StringHelper.BodyArmorLegendary, GlobalRarity.Legendary, Rnd!.RandfRange(450, 600), Rnd.RandfRange(600, 900), ResourcePath.BodyArmorLegendary, GD.Load<Texture2D>(TexturePaths.BodyArmorLegendary), 1, 1),
                _ => null,
            };
        }
    }
}

namespace Playground.Script.Items.Factories
{
    using Playground.Script.Items.UniqItems;
    using Playground.Script.LootGenerator.BasedOnRarityLootGenerator;

    public class BodyArmorFactory : ItemCreator
    {
        private static BodyArmorFactory instance = null;

        private BodyArmorFactory()
        {

        }

        public static BodyArmorFactory Instance
        {
            get
            {
                instance ??= new BodyArmorFactory();
                return instance;
            }
        }

        public override BodyArmor GenerateItem(GlobalRarity rarity)
        {
            // потенциальный memory leak
            return rarity switch
            {
                GlobalRarity.Uncommon => new BodyArmor("Iron BodyArmor", GlobalRarity.Uncommon, RandomNumberGenerator.RandfRange(160, 220), RandomNumberGenerator.RandfRange(65, 80), string.Empty, null, 1, 1),
                GlobalRarity.Rare => new BodyArmor("Silver BodyArmor", GlobalRarity.Rare, RandomNumberGenerator.RandfRange(200, 280), RandomNumberGenerator.RandfRange(70, 120), string.Empty, null, 1, 1),
                GlobalRarity.Epic => new("Golden BodyArmor", GlobalRarity.Epic, RandomNumberGenerator.RandfRange(260, 320), RandomNumberGenerator.RandfRange(130, 160), string.Empty, null, 1, 1),
                GlobalRarity.Legendary => new BodyArmor("Phoenix BodyArmor", GlobalRarity.Legendary, RandomNumberGenerator.RandfRange(450, 600), RandomNumberGenerator.RandfRange(180, 300), string.Empty, null, 1, 1),
                GlobalRarity.Mythic => VeryUniqBodyArmor.Instance,
                _ => null,
            };
        }
    }
}

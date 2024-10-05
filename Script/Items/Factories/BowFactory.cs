using Playground.Script.LootGenerator.BasedOnRarityLootGenerator;

namespace Playground.Script.Items.Factories
{
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
            return globalRarity switch
            {
                GlobalRarity.Common => new Bow("Bronze Bow", GlobalRarity.Common, RandomNumberGenerator.RandfRange(25, 50), RandomNumberGenerator.RandfRange(100, 150), string.Empty, null, 1, 1),
                GlobalRarity.Uncommon => new Bow("Iron Bow", GlobalRarity.Uncommon, RandomNumberGenerator.RandfRange(65, 80), RandomNumberGenerator.RandfRange(160, 220), string.Empty, null, 1, 1),
                GlobalRarity.Rare => new Bow("Silver Bow", GlobalRarity.Rare, RandomNumberGenerator.RandfRange(70, 120), RandomNumberGenerator.RandfRange(200, 280), string.Empty, null, 1, 1),
                GlobalRarity.Epic => new Bow("Golden Bow", GlobalRarity.Epic, RandomNumberGenerator.RandfRange(130, 160), RandomNumberGenerator.RandfRange(260, 320), string.Empty, null, 1, 1),
                GlobalRarity.Legendary => new Bow("Phoenix Bow", GlobalRarity.Legendary, RandomNumberGenerator.RandfRange(180, 300), RandomNumberGenerator.RandfRange(450, 600), string.Empty, null, 1, 1),
                GlobalRarity.Mythic => VeryUniqBow.Instance,
                _ => null,
            };
        }
    }
}

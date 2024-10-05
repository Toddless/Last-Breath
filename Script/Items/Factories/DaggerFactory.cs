using Playground.Script.LootGenerator.BasedOnRarityLootGenerator;

namespace Playground.Script.Items.Factories
{
    public class DaggerFactory : ItemCreator
    {
        private static DaggerFactory instance = null;

        private DaggerFactory()
        {

        }

        public static DaggerFactory Instance
        {
            get
            {
                instance ??= new DaggerFactory();
                return instance;
            }
        }

        public override Dagger GenerateItem(GlobalRarity rarity)
        {
            return rarity switch
            {
                GlobalRarity.Common => new Dagger("Dagger Bronze", GlobalRarity.Common, RandomNumberGenerator.RandfRange(30, 45), RandomNumberGenerator.RandfRange(140, 160), string.Empty, null, 1, 1),
                GlobalRarity.Uncommon => new Dagger("Dagger Kooper", GlobalRarity.Uncommon, RandomNumberGenerator.RandfRange(75, 100), RandomNumberGenerator.RandfRange(170, 195), string.Empty, null, 1, 1),
                GlobalRarity.Rare => new Dagger("Dagger Silver", GlobalRarity.Rare, RandomNumberGenerator.RandfRange(170, 200), RandomNumberGenerator.RandfRange(250, 300), string.Empty, null, 1, 1),
                GlobalRarity.Epic => new Dagger("Dagger Gold", GlobalRarity.Epic, RandomNumberGenerator.RandfRange(200, 250), RandomNumberGenerator.RandfRange(300, 380), string.Empty, null, 1, 1),
                GlobalRarity.Legendary => new Dagger("Dagger Phoenix Fire", GlobalRarity.Legendary, RandomNumberGenerator.RandfRange(300, 380), RandomNumberGenerator.RandfRange(400, 450), string.Empty, null, 1, 1),
                GlobalRarity.Mythic => VeryUniqDagger.Instance,
                _ => null,
            };
        }
    }
}

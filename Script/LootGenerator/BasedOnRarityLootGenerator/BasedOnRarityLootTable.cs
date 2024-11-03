namespace Playground.Script.LootGenerator.BasedOnRarityLootGenerator
{
    public class BasedOnRarityLootTable : GenericObjectsTable<RarityLoodDrop, Rarity>
    {
        private static BasedOnRarityLootTable? instance = null;

        private BasedOnRarityLootTable()
        {

        }

        public static BasedOnRarityLootTable Instance
        {
            get
            {
                instance ??= new BasedOnRarityLootTable();
                return instance;
            }
        }

        public void InitializeLootTable()
        {
            lootDropItems =
            [
                new RarityLoodDrop(new Rarity(),GlobalRarity.Uncommon),
                new RarityLoodDrop(new Rarity(),GlobalRarity.Rare),
                new RarityLoodDrop(new Rarity(),GlobalRarity.Epic),
                new RarityLoodDrop(new Rarity(),GlobalRarity.Legendary),
                new RarityLoodDrop(new Rarity(),GlobalRarity.Mythic),
            ];
        }
    }
}

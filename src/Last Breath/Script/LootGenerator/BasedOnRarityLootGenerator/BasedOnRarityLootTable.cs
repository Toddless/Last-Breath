namespace Playground.Script.LootGenerator.BasedOnRarityLootGenerator
{
    using Playground.Script.Enums;
    using Playground.Script.Items;

    public class BasedOnRarityLootTable : GenericObjectsTable<RarityLoodDrop, Rarity>, IBasedOnRarityLootTable
    {
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

        public override void ValidateTable() => base.ValidateTable();

        public override RarityLoodDrop? GetRarity() => base.GetRarity();

        public override Item? GetRandomItem() => base.GetRandomItem();

        public override Item? GetItemWithSelectedRarity(int index) => base.GetItemWithSelectedRarity(index);
    }
}

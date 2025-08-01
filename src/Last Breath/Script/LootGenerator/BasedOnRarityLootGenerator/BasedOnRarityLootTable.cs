namespace LastBreath.Script.LootGenerator.BasedOnRarityLootGenerator
{
    using LastBreath.Script.LootGenerator;
    using LastBreath.Script.Items;
    using Contracts.Enums;

    public class BasedOnRarityLootTable : GenericObjectsTable<Rarity>, IBasedOnRarityLootTable
    {
        public void InitializeLootTable()
        {
            LootDropItems =
            [
                new Rarity(GlobalRarity.Uncommon),
                new Rarity(GlobalRarity.Rare),
                new Rarity(GlobalRarity.Epic),
                new Rarity(GlobalRarity.Legendary),
            ];
        }

        public override void ValidateTable() => base.ValidateTable();

        public override Rarity? GetRarity() => base.GetRarity();

        public override Item GetRandomItem() => base.GetRandomItem();

        public override Item? GetItemWithSelectedRarity(int index) => base.GetItemWithSelectedRarity(index);
    }
}

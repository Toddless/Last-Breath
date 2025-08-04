namespace LastBreath.Script.LootGenerator.BasedOnRarityLootGenerator
{
    using LastBreath.Script.LootGenerator;
    using LastBreath.Script.Items;

    public class BasedOnRarityLootTable : GenericObjectsTable<Rarity>, IBasedOnRarityLootTable
    {
        public void InitializeLootTable()
        {
            LootDropItems =
            [
                new Rarity(Contracts.Enums.Rarity.Uncommon),
                new Rarity(Contracts.Enums.Rarity.Rare),
                new Rarity(Contracts.Enums.Rarity.Epic),
                new Rarity(Contracts.Enums.Rarity.Legendary),
            ];
        }

        public override void ValidateTable() => base.ValidateTable();

        public override Rarity? GetRarity() => base.GetRarity();

        public override Item GetRandomItem() => base.GetRandomItem();

        public override Item? GetItemWithSelectedRarity(int index) => base.GetItemWithSelectedRarity(index);
    }
}

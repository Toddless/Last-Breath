namespace LastBreath.Script.LootGenerator.BasedOnRarityLootGenerator
{
    using LastBreath.Script.Items;

    public interface IBasedOnRarityLootTable
    {
        Item? GetItemWithSelectedRarity(int index);
        Item GetRandomItem();
        Rarity? GetRarity();
        void InitializeLootTable();
        void ValidateTable();
    }
}

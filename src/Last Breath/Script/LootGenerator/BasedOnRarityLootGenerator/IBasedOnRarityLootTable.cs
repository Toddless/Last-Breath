namespace Playground.Script.LootGenerator.BasedOnRarityLootGenerator
{
    using Playground.Script.Items;

    public interface IBasedOnRarityLootTable
    {
        Item? GetItemWithSelectedRarity(int index);
        Item? GetRandomItem();
        RarityLoodDrop? GetRarity();
        void InitializeLootTable();
        void ValidateTable();
    }
}
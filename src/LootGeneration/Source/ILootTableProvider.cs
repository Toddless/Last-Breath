namespace LootGeneration.Source
{
    using System.Collections.Generic;
    using Core.Data.LootTable;

    public interface ILootTableProvider
    {
        List<LootTableTierData> BasicTable { get; }
        List<LootTableTierData> GetLootTable<TKey>(TKey key);
    }
}

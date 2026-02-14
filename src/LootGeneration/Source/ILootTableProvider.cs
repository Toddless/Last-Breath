namespace LootGeneration.Source
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.Data.LootTable;

    public interface ILootTableProvider
    {
        List<LootTableTierData> BasicTable { get; }
        List<LootTableTierData> GetLootTable<TKey>(TKey key);
        void LoadData();
    }
}

namespace LootGeneration.Source
{
    using System.Linq;
    using Core.Data.LootTable;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using Core.Interfaces.MessageBus;

    public class GetLootTableRequestHandler(ILootTableProvider lootTableProvider) : IRequestHandler<GetLootTableRequest, Dictionary<int, List<TableRecord>>>
    {
        public Task<Dictionary<int, List<TableRecord>>> HandleRequest(GetLootTableRequest request)
        {
            var fractionTable = lootTableProvider.GetLootTable(request.Fraction);
            var entityTable = lootTableProvider.GetLootTable(request.Type);
            var individualTable = lootTableProvider.GetLootTable(request.Id);
            var basicTable = lootTableProvider.BasicTable;

            var combined = new Dictionary<int, List<TableRecord>>();
            for (int i = 0; i < basicTable.Count; i++)
            {
                GetLootTableRecords(fractionTable, i, out var fractionRecords);
                GetLootTableRecords(entityTable, i, out var entityTypeRecords);
                GetLootTableRecords(individualTable, i, out var individualRecords);
                var basic = basicTable.FirstOrDefault(x => x.Tier == i)?.Items ?? [];
                var combinedRecords = fractionRecords.Union(entityTypeRecords).Union(individualRecords).Union(basic).ToList();
                combined.TryAdd(i, combinedRecords);
            }

            return Task.FromResult(combined);
        }

        private void GetLootTableRecords(List<LootTableTierData>? value, int tier, out List<TableRecord> records) =>
            records = value != null && value.Exists(x => x.Tier == tier) ? records = value[tier].Items : records = [];
    }
}

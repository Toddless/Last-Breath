namespace LootGeneration.Source
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Data.LootTable;
    using Core.MessageBus;

    public class GetLootTableRequestHandler(ILootTableProvider lootTableProvider) : IRequestHandler<GetLootTableRequest, Dictionary<int, List<TableRecord>>>
    {
        public Task<Dictionary<int, List<TableRecord>>> HandleRequest(GetLootTableRequest request)
        {
            List<List<LootTableTierData>> sources =
            [
                lootTableProvider.GetLootTable(request.Fraction),
                lootTableProvider.GetLootTable(request.Type),
                lootTableProvider.GetLootTable(request.Id),
                lootTableProvider.BasicTable
            ];

            // Tiers are looked up by their declared number, never by list position, so partial or unordered
            // tables are fine. Combined lists are fresh copies: callers may append to them without touching
            // the providers' cached data.
            var combined = new Dictionary<int, List<TableRecord>>();
            foreach (var tierData in sources.SelectMany(source => source))
            {
                if (!combined.TryGetValue(tierData.Tier, out var records)) combined[tierData.Tier] = records = [];
                records.AddRange(tierData.Items.Where(item => !records.Contains(item)));
            }

            return Task.FromResult(combined);
        }
    }
}

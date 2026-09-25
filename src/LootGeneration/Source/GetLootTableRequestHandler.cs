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
            // the providers' cached data. The union counts SEATS: a record naming a set of augments joins
            // the tier as one entry, however many augments end up answering it, and two sources naming the
            // same set at the same price merge into that one entry like two sources naming the same id.
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

namespace LootGeneration.Source
{
    using Godot;
    using System;
    using Utilities;
    using Core.Enums;
    using System.Linq;
    using Core.Data.LootTable;
    using System.Threading.Tasks;
    using System.Collections.Generic;

    public class LootTableProvider : ILootTableProvider
    {
        private const string DataPath = "res://Data/LootTables/";
        private Dictionary<Fractions, List<LootTableTierData>> _fractionTables = [];
        private Dictionary<EntityType, List<LootTableTierData>> _entityTypeTables = [];
        private Dictionary<string, List<LootTableTierData>> _individualTables = [];
        private List<LootTableTierData> _basicTable = [];

        public List<LootTableTierData> BasicTable => _basicTable.ToList();

        public List<LootTableTierData> GetLootTable<TKey>(TKey key)
        {
            List<LootTableTierData>? table = key switch
            {
                Fractions fractions => _fractionTables.GetValueOrDefault(fractions),
                EntityType entityType => _entityTypeTables.GetValueOrDefault(entityType),
                string individual => _individualTables.GetValueOrDefault(individual),
                _ => null
            };

            if (table != null)
            {
                return table.ToList();
            }

            Tracker.TrackError("Could not find loot table for " + key);
            GD.Print("Could not find loot table for " + key);
            return [];
        }

        public async void LoadData()
        {
            try
            {
                await DataLoader.LoadDataFromJson(DataPath, async s => await ParseTables(s));
            }
            catch (Exception exception)
            {
                GD.Print($"Failed to load loot table data from {DataPath}: {exception.Message}, {exception.StackTrace}");
                Tracker.TrackException($"Failed to load loot table data", exception, this);
            }
        }

        private async Task ParseTables(string jsonContent) =>
            await DataParser.ParseLootTables(jsonContent, ref _fractionTables, ref _entityTypeTables, ref _individualTables, ref _basicTable);
    }
}

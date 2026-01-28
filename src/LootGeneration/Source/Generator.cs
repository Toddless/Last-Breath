namespace LootGeneration.Source
{
    using Test;
    using Core.Enums;
    using Core.Interfaces.Events;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using Core.Data;
    using Core.Data.LootTable;
    using Utilities;

    public class Generator
    {
        private const float LvlCoefficient = 0.35f;

        private readonly float[] _baseTierChances = [0.005f, 0.035f, 0.15f, 0.81f];
        private readonly float[] _baseRarityChances = [0.001f, 0.04f, 0.30f, 0.65f];

        private readonly Dictionary<EntityType, float> _baseBudget = new()
        {
            [EntityType.Regular] = 1f,
            [EntityType.Special] = 2f,
            [EntityType.Elit] = 4f,
            [EntityType.Unique] = 7f,
            [EntityType.Boss] = 15f,
            [EntityType.Archon] = 30f
        };

        private readonly IItemDataProvider _dataProvider;
        private readonly IGameEventBus _eventBus;
        private const string DataPath = "res://Data/LootTables/";
        private Dictionary<Fractions, List<LootTableTierData>> _fractionTables = [];
        private Dictionary<EntityType, List<LootTableTierData>> _entityTypeTables = [];
        private Dictionary<string, List<LootTableTierData>> _individualTables = [];
        private List<LootTableTierData> _basicTable = [];

        public Generator(IItemDataProvider itemDataProvider, IGameEventBus gameEventBus)
        {
            _dataProvider = itemDataProvider;
            _eventBus = gameEventBus;
            Subscribe();
        }

        // TODO:
        // Npc generation interface - type, fraction, rarity, level, random or determined modifiers
        // Item - effect on drop. Where it drop??
        // How to recalculate all probabilities for rarity and tiers?
        // Logic within all npc modifiers
        //


        public async Task LoadData()
        {
            await DataLoader.LoadDataFromJson(DataPath, async s => await ParseTables(s));
        }

        private async Task ParseTables(string jsonContent) =>
            await DataParser.ParseLootTables(jsonContent, ref _fractionTables, ref _entityTypeTables, ref _individualTables, ref _basicTable);

        private void Subscribe()
        {
        }
    }
}

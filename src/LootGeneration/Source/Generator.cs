namespace LootGeneration.Source
{
    using Test;
    using Godot;
    using System;
    using Utilities;
    using Core.Data;
    using Core.Enums;
    using System.Linq;
    using Core.Interfaces;
    using Core.Data.LootTable;
    using Core.Interfaces.Items;
    using Core.Interfaces.Events;
    using System.Threading.Tasks;
    using Core.Interfaces.Entity;
    using System.Collections.Generic;
    using Core.Interfaces.Events.GameEvents;

    public class Generator
    {
        private const string DataPath = "res://Data/LootTables/";
        private const float LvlCoefficient = 0.35f;
        private const float EquipItemEffectChance = 0.25f;

        private readonly int[] _tierPrices = [45, 30, 15, 5];
        private readonly float[] _baseTierChances = [0.005f, 0.035f, 0.15f, 0.81f];
        private readonly float[] _baseRarityChances = [0.001f, 0.04f, 0.30f, 0.65f];
        private readonly RandomNumberGenerator _rnd = new();
        private readonly IItemDataProvider _dataProvider;
        private readonly IGameEventBus _eventBus;
        private readonly ExampleItemEffectProvider _effectProvider = new();
        private readonly List<IItem> _itemsToDrop = [];
        private readonly List<string> _chosenItemsIds = [];

        private readonly Dictionary<EntityType, float> _baseBudget = new()
        {
            [EntityType.Regular] = 1f,
            [EntityType.Special] = 2f,
            [EntityType.Elit] = 4f,
            [EntityType.Unique] = 7f,
            [EntityType.Boss] = 15f,
            [EntityType.Archon] = 30f
        };

        private readonly Dictionary<Rarity, float> _rarityMultipliers = new()
        {
            [Rarity.Uncommon] = 1.0f,
            [Rarity.Rare] = 1.25f,
            [Rarity.Epic] = 1.5f,
            [Rarity.Legendary] = 2f,
            [Rarity.Unique] = 2.8f,
            [Rarity.Mythic] = 4f
        };

        private readonly Dictionary<string, Dictionary<int, List<TableRecord>>> _tableCache = [];
        private Dictionary<Fractions, List<LootTableTierData>> _fractionTables = [];
        private Dictionary<EntityType, List<LootTableTierData>> _entityTypeTables = [];
        private Dictionary<string, List<LootTableTierData>> _individualTables = [];
        private List<LootTableTierData> _basicTable = [];

        public Generator(IItemDataProvider itemDataProvider, IGameEventBus gameEventBus)
        {
            _dataProvider = itemDataProvider;
            _eventBus = gameEventBus;
            _rnd.Randomize();
            Subscribe();
        }

        // TODO:
        // Npc generation interface - type, fraction, rarity, level, random or determined modifiers
        // Item - effect on drop. Where it drop??


        public async Task LoadData()
        {
            await DataLoader.LoadDataFromJson(DataPath, async s => await ParseTables(s));
        }

        public void GenerateItemsNearPlayer()
        {
        }

        private async Task ParseTables(string jsonContent) =>
            await DataParser.ParseLootTables(jsonContent, ref _fractionTables, ref _entityTypeTables, ref _individualTables, ref _basicTable);

        private void Subscribe()
        {
            _eventBus.Subscribe<EntityDiedEvent>(OnEntityDied);
            _eventBus.Subscribe<BattleEndEvent>(OnBattleEnd);
        }

        private void OnBattleEnd(BattleEndEvent obj)
        {
            _tableCache.Clear();
            _chosenItemsIds.Clear();
            _itemsToDrop.Clear();
        }

        private void OnEntityDied(EntityDiedEvent evnt)
        {
            // also return if died entity is Player
            // npc can die multiple times. Each time it generate loot
            // loot should drop on battle ends


            if (evnt.Entity is not ExampleNpc npc) return;

            ApplyNpcModifiers(npc, out IModifierApplyingContext context);

            CopyBaseTierChances(out float[] tierChances);

            float budget = CalculateBudget(npc);

            var npcModifiers = npc.NpcModifiers.ToList();

            float[] actualTierChances = Calculations.CalculateChances<ITierMultiplierModifier>(npcModifiers, tierChances);

            // same npc can die multiple times per battle
            if (!_tableCache.TryGetValue(npc.InstanceId, out Dictionary<int, List<TableRecord>>? baseTable))
            {
                baseTable = CreateLootTable(npc);
                _tableCache[npc.InstanceId] = baseTable;
            }

            var modifiedTable = baseTable.Union(context.AdditionalItems).ToDictionary(x => x.Key, x => x.Value);

            while (budget > 1f)
            {
                int chosenTier = MakeRoll(actualTierChances);
                chosenTier = context.TryUpgradeTier(chosenTier, _rnd.Randf());

                if (_tierPrices[chosenTier] > budget) continue;

                if (!modifiedTable.TryGetValue(chosenTier, out var tableRecords)) continue;

                var randomItem = tableRecords[_rnd.RandiRange(0, tableRecords.Count)];
                tableRecords.Remove(randomItem);
                budget -= randomItem.Price;
                _chosenItemsIds.Add(randomItem.Id);
            }

            _chosenItemsIds.AddRange(context.GuaranteedItems);

            if (budget > 0) ConvertLeftBudgetToGold(budget);

            CopyBaseRarityChances(out float[] rarityChances);
            float[] actualRarityChances = Calculations.CalculateChances<IRarityUpgradeModifier>(npcModifiers, rarityChances);
            GenerateChosenItems(actualRarityChances, context);
        }

        private void GenerateChosenItems(float[] actualRarityChances, IModifierApplyingContext context)
        {
            try
            {
                foreach (var item in _chosenItemsIds.Select(chosenItemId => _dataProvider.CopyBaseItem(chosenItemId)))
                {
                    if (item is IEquipItem equip) HandleEquipItemGeneration(equip, actualRarityChances, context);

                    _itemsToDrop.Add(item);
                }
            }
            catch (Exception ex)
            {
                Tracker.TrackException($"Failed to generate chosen items.", ex, this);
            }
        }

        private void HandleEquipItemGeneration(IEquipItem equip, float[] actualRarityChances, IModifierApplyingContext context)
        {
            if (equip.Rarity is Rarity.Mythic or Rarity.Unique) return;

            Rarity rarityRoll = (Rarity)MakeRoll(actualRarityChances);
            rarityRoll = context.TryUpgradeRarity(rarityRoll);
            // concat all item effects with effects from context
            var allEffects = _effectProvider.GetCopyItemsEffects().Concat(context.AdditionalItemEffects).ToList();
            string equipItemEffect = _rnd.Randf() <= EquipItemEffectChance
                ? allEffects[_rnd.RandiRange(0, allEffects.Count)]
                : string.Empty;
            equip.SetItemEffect(equipItemEffect);
            var modifiersPool = _dataProvider.GetEquipItemModifierPool(equip.Id);
            // Don't forget to concat item modifiers with modifier from context
            var weighted = WeightedRandomPicker.CalculateWeights(modifiersPool);
            var chosenMods = WeightedRandomPicker.PickRandomMultipleWithoutDuplicate(
                weighted.WeightedObjects,
                weighted.TotalWeight,
                rarityRoll.ConvertRarityToItemModifierAmount(),
                _rnd);
            equip.SetAdditionalModifiers(chosenMods);
        }

        private void ConvertLeftBudgetToGold(float budget)
        {
        }

        private int MakeRoll(float[] chances)
        {
            float roll = _rnd.Randf();
            float cumulative = 0f;

            for (int i = 0; i < chances.Length; i++)
            {
                cumulative += chances[i];
                if (roll < cumulative)
                    return i;
            }

            return chances.Length - 1;
        }

        private Dictionary<int, List<TableRecord>> CreateLootTable(ExampleNpc npc)
        {
            _fractionTables.TryGetValue(npc.Fraction, out List<LootTableTierData>? table);
            _entityTypeTables.TryGetValue(npc.EntityType, out List<LootTableTierData>? entityType);
            _individualTables.TryGetValue(npc.Id, out List<LootTableTierData>? individual);

            var combined = new Dictionary<int, List<TableRecord>>();
            for (int i = 0; i < _basicTable.Count; i++)
            {
                GetLootTableRecords(table, i, out var fractionRecords);
                GetLootTableRecords(entityType, i, out var entityTypeRecords);
                GetLootTableRecords(individual, i, out var individualRecords);
                var combinedRecords = fractionRecords.Union(entityTypeRecords).Union(individualRecords).ToList();
                combined.TryAdd(i, combinedRecords);
            }

            return combined;
        }

        private void GetLootTableRecords(List<LootTableTierData>? value, int tier, out List<TableRecord> records) =>
            records = value != null && value.Exists(x => x.Tier == tier) ? records = value[tier].Items : records = [];

        private void CopyBaseTierChances(out float[] tierChances)
        {
            tierChances = new float[_baseTierChances.Length];
            _baseTierChances.CopyTo(tierChances, 0);
        }

        private void CopyBaseRarityChances(out float[] rarityChances)
        {
            rarityChances = new float[_baseRarityChances.Length];
            _baseRarityChances.CopyTo(rarityChances, 0);
        }

        private void ApplyNpcModifiers(ExampleNpc npc, out IModifierApplyingContext modifierApplyingContext)
        {
            modifierApplyingContext = new ExampleModifierApplyingContext();
            foreach (INpcModifier npcNpcModifier in npc.NpcModifiers)
                npcNpcModifier.ApplyModifier(modifierApplyingContext);
        }


        private float CalculateBudget(ExampleNpc npc)
        {
            float baseBudget = _baseBudget.GetValueOrDefault(npc.EntityType, 1f);
            float rarityMultiplier = _rarityMultipliers.GetValueOrDefault(npc.Rarity, 1f);
            float difficultyMultiplier = npc.NpcModifiers.Sum(mod => mod.DifficultyMultiplier);
            float f = (int)npc.EntityType > 3 ? Mathf.Log(npc.Level + 1) : Mathf.Sqrt(npc.Level);
            return baseBudget * (1 + LvlCoefficient * f) * rarityMultiplier * difficultyMultiplier;
        }
    }
}

namespace LootGeneration.Source
{
    using Godot;
    using System;
    using Internal;
    using Utilities;
    using Core.Enums;
    using System.Linq;
    using Core.Interfaces;
    using Core.Data.LootTable;
    using Core.Interfaces.Items;
    using Core.Interfaces.Events;
    using System.Threading.Tasks;
    using Core.Interfaces.Entity;
    using System.Collections.Generic;
    using Core.Interfaces.MessageBus;
    using Core.Interfaces.Events.GameEvents;

    public class LootGenerationService : ILootGenerationService
    {
        private readonly Dictionary<string, Dictionary<int, List<TableRecord>>> _tableCache = [];
        private readonly RandomNumberGenerator _rnd;
        private readonly IGameMessageBus _gameMessageBus;
        private readonly IGameEventBus _gameEventBus;
        private readonly IItemCreationService _itemCreationService;
        private ILootConfiguration _configuration;

        public LootGenerationService(
            RandomNumberGenerator rnd,
            IGameEventBus eventBus,
            IGameMessageBus messageBus,
            IItemCreationService itemCreationService,
            ILootConfiguration configuration)
        {
            _rnd = rnd;
            _configuration = configuration;
            _gameEventBus = eventBus;
            _gameMessageBus = messageBus;
            _itemCreationService = itemCreationService;
            _gameEventBus.Subscribe<BattleEndEvent>(OnBattleEnds);
        }

        public void ChangeLootConfiguration(ILootConfiguration configuration) => _configuration = configuration;

        public async Task<List<ItemStack>> GenerateItemsAsync(IEntity diedEntity)
        {
            if (diedEntity is not INpc npc) return [];

            float budget = CalculateBudget(npc);
            _gameEventBus.Publish(new BudgetCalculatedEvent(budget));

            // same npc can die multiple times per battle
            if (!_tableCache.TryGetValue(npc.InstanceId, out Dictionary<int, List<TableRecord>>? baseTable))
            {
                baseTable = await _gameMessageBus.Send<GetLootTableRequest, Dictionary<int, List<TableRecord>>>(new(npc.Fraction, npc.EntityType, npc.Id));
                _tableCache[npc.InstanceId] = baseTable;
            }

            var context = CreateModifierApplyingContext(npc);
            var finalLootTable = CreateFinalLootTable(baseTable, context.AdditionalItems);
            var npcModifiers = npc.NpcModifiers.AllModifiers;

            float[] actualTierChances = Calculations.CalculateChances<ITierMultiplierModifier>(npcModifiers, CopyBaseChances(_configuration.BaseTierChances));
            float[] actualRarityChances = Calculations.CalculateChances<IRarityUpgradeModifier>(npcModifiers, CopyBaseChances(_configuration.BaseRarityChances));

            var chosenItemsIds = SpendBudget(budget, _configuration.TierPrices, actualTierChances, context.TryUpgradeTier, finalLootTable);
            chosenItemsIds.AddRange(context.GuaranteedItems);

            return GenerateChosenItems(actualRarityChances, context, chosenItemsIds);
        }

        private Dictionary<int, List<TableRecord>> CreateFinalLootTable(Dictionary<int, List<TableRecord>> baseTable, Dictionary<int, List<TableRecord>> additionalItems)
        {
            var lootTable = new Dictionary<int, List<TableRecord>>(baseTable);
            foreach (var kvp in additionalItems)
            {
                if (lootTable.TryGetValue(kvp.Key, out var existingList)) existingList.AddRange(kvp.Value);
                else lootTable[kvp.Key] = [..kvp.Value];
            }

            return lootTable;
        }

        private List<string> SpendBudget(
            float budget,
            int[] tierPrices,
            float[] actualTierChances,
            Func<int, float, int> tryUpgradeTier,
            Dictionary<int, List<TableRecord>> modifiedTable)
        {
            Dictionary<int, int> tiersAmount = new() { [0] = 0, [1] = 0, [2] = 0, [3] = 0 };
            List<string> chosenItemsIds = [];
            while (budget > tierPrices[^1])
            {
                int chosenTier = MakeRoll(actualTierChances);
                chosenTier = tryUpgradeTier(chosenTier, _rnd.Randf());

                if (chosenTier < 0 || chosenTier >= tierPrices.Length) continue;
                if (tierPrices[chosenTier] > budget) chosenTier = FindFirstSuitableTier(budget);
                if (!modifiedTable.TryGetValue(chosenTier, out var tableRecords) || tableRecords.Count == 0) continue;

                int randomNumber = _rnd.RandiRange(0, tableRecords.Count - 1);
                var randomItem = tableRecords[randomNumber];

                if (randomItem.Price > budget) randomItem = FindFirstSuitableItem(tableRecords, budget);
                if (randomItem == null || string.IsNullOrWhiteSpace(randomItem.Id)) continue;
                tiersAmount[chosenTier]++;
                budget -= randomItem.Price;
                chosenItemsIds.Add(randomItem.Id);
            }

            _gameEventBus.Publish<ItemTierChosenEvent>(new(tiersAmount));

            return chosenItemsIds;
        }

        private List<ItemStack> GenerateChosenItems(float[] actualRarityChances, IModifierApplyingContext context, List<string> chosenItemsIds)
        {
            var items = new List<ItemStack>();
            var rarityAmount = Enum.GetValues<Rarity>().ToDictionary(x => x, _ => 0);
            try
            {
                _gameEventBus.Publish<ChosenItemIds>(new(chosenItemsIds.ToList()));
                foreach (string id in chosenItemsIds)
                {
                    if (string.IsNullOrWhiteSpace(id)) continue;

                    // looking for stackable item
                    var existingItemStack = items.FirstOrDefault(itemStack => itemStack.Item.Id == id && itemStack.Item is not IEquipItem);

                    if (existingItemStack != null)
                    {
                        existingItemStack.Stack++;
                        rarityAmount[existingItemStack.Item.Rarity]++;
                        continue;
                    }

                    Rarity rarity = context.TryUpgradeRarity((Rarity)MakeRoll(actualRarityChances));
                    var item = _itemCreationService.CreateItem(id, context.AdditionalItemEffects, rarity, _configuration.EquipItemEffectChance);
                    rarityAmount[item.Rarity]++;
                    items.Add(new ItemStack(item) { Stack = 1 });
                }

                _gameEventBus.Publish(new EquipRarityChosenEvent(rarityAmount));
                return items;
            }
            catch (Exception ex)
            {
                Tracker.TrackException($"Failed to generate chosen items.", ex, this);
                return items;
            }
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

        private int FindFirstSuitableTier(float budget)
        {
            int[] tierPrices = _configuration.TierPrices;
            for (int i = 0; i < tierPrices.Length; i++)
                if (tierPrices[i] < budget)
                    return i;

            return tierPrices.Length - 1;
        }

        private TableRecord? FindFirstSuitableItem(List<TableRecord> tableRecords, float budget) => tableRecords.FirstOrDefault(x => x.Price >= budget);

        private void OnBattleEnds(BattleEndEvent obj) => _tableCache.Clear();

        private float[] CopyBaseChances(float[] baseChancesToCopy)
        {
            float[] tierChances = new float[_configuration.BaseTierChances.Length];
            baseChancesToCopy.CopyTo(tierChances, 0);
            return tierChances;
        }

        private IModifierApplyingContext CreateModifierApplyingContext(INpc npc)
        {
            var modifierApplyingContext = new ModifierApplyingContext();
            foreach (INpcModifier npcNpcModifier in npc.NpcModifiers.AllModifiers)
                npcNpcModifier.ApplyModifier(modifierApplyingContext);
            return modifierApplyingContext;
        }

        private float CalculateBudget(INpc npc)
        {
            float baseBudget = _configuration.BaseBudget.GetValueOrDefault(npc.EntityType, 1f);
            float rarityMultiplier = _configuration.RarityMultipliers.GetValueOrDefault(npc.Rarity, 1f);
            float difficultyMultiplier = npc.NpcModifiers.AllModifiers.Sum(mod => mod.DifficultyMultiplier);
            float f = (int)npc.EntityType > 3 ? Mathf.Log(npc.Level + 1) : Mathf.Sqrt(npc.Level);
            return baseBudget * (1 + _configuration.LvlCoefficient * f) * rarityMultiplier * (1 + difficultyMultiplier);
        }
    }
}

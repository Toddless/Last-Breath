namespace LootGeneration.Source
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core;
    using Core.Context;
    using Core.Data.LootTable;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Events;
    using Core.Items;
    using Core.MessageBus;
    using Core.Services;
    using Godot;

    /// <summary>The drop pipeline: listens for entity deaths, rolls that entity's loot table (cached per id),
    /// then asks <see cref="IItemCreationService"/> to spawn the resulting items and publishes them on the buses.
    /// Odds, entity- and rarity-weights all come from the injected <see cref="ILootConfiguration"/>.</summary>
    public class LootGenerationService : ILootGenerationService
    {
        private readonly Dictionary<string, Dictionary<int, List<TableRecord>>> _tableCache = [];
        private readonly IRandomNumberGenerator _rnd;
        private readonly IGameMessageBus _gameMessageBus;
        private readonly IGameEventBus _gameEventBus;
        private readonly IItemCreationService _itemCreationService;
        private readonly List<string> _diedEntities = [];
        private ILootConfiguration _configuration;

        public LootGenerationService(
            IRandomNumberGenerator rnd,
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

        public async Task<List<ItemStack>> GenerateItemsAsync(IFightable diedEntity)
        {
            // A summon is a spell manifestation, not a creature: it drops nothing by design.
            if (diedEntity is not IFightableNpc { IsSummon: false } npc || _diedEntities.Contains(diedEntity.InstanceId)) return [];

            float budget = CalculateBudget(npc);

            // same npc can die multiple times per battle
            if (!_tableCache.TryGetValue(npc.InstanceId, out Dictionary<int, List<TableRecord>>? baseTable))
            {
                baseTable = await _gameMessageBus.SendRequest<GetLootTableRequest, Dictionary<int, List<TableRecord>>>(new(npc.Fraction, npc.EntityType, npc.Id));
                _tableCache[npc.InstanceId] = baseTable;
            }

            var context = CreateModifierApplyingContext(npc);
            var finalLootTable = CreateFinalLootTable(baseTable, context.AdditionalItems);
            var npcModifiers = npc.NpcModifiers.AllModifiers;

            float[] actualTierChances = Calculations.CalculateChances<ITierMultiplierModifier>(npcModifiers, CopyBaseChances(_configuration.BaseTierChances));
            float[] actualRarityChances = Calculations.CalculateChances<IRarityUpgradeModifier>(npcModifiers, CopyBaseChances(_configuration.BaseRarityChances));

            var chosenItemsIds = SpendBudget(budget, _configuration.TierPrices, actualTierChances, context.TryUpgradeTier, finalLootTable);
            chosenItemsIds.AddRange(context.GuaranteedItems);
            _diedEntities.Add(diedEntity.InstanceId);
            return GenerateChosenItems(actualRarityChances, context, chosenItemsIds);
        }

        private Dictionary<int, List<TableRecord>> CreateFinalLootTable(Dictionary<int, List<TableRecord>> baseTable, Dictionary<int, List<TableRecord>> additionalItems)
        {
            // TODO:
            // Мутация кэша.
            var lootTable = new Dictionary<int, List<TableRecord>>(baseTable);
            foreach (var kvp in additionalItems)
            {
                if (lootTable.TryGetValue(kvp.Key, out var existingList)) existingList.AddRange(kvp.Value);
                else lootTable[kvp.Key] = [..kvp.Value];
            }

            return lootTable;
        }

        private const int MaxWastedRolls = 100;

        private List<string> SpendBudget(
            float budget,
            int[] tierPrices,
            float[] actualTierChances,
            Func<int, float, int> tryUpgradeTier,
            Dictionary<int, List<TableRecord>> modifiedTable)
        {
            Dictionary<int, int> tiersAmount = Enumerable.Range(0, tierPrices.Length).ToDictionary(tier => tier, _ => 0);
            List<(TableRecord Record, int Tier)> chosen = [];

            // Tier affordability follows the ACTUAL cheapest item in the table, not the configured
            // band price: a tier whose items start at 250 must be buyable with a 300 budget.
            var minPriceByTier = BuildMinPriceByTier(modifiedTable);
            float cheapestPrice = minPriceByTier.Count > 0 ? minPriceByTier.Values.Min() : float.MaxValue;
            int maxItems = _configuration.MaxItemsPerKill > 0 ? _configuration.MaxItemsPerKill : int.MaxValue;

            // A roll can fail without spending budget (empty tier, no affordable item) — cap those so a
            // sparse loot table can never spin this loop forever.
            int wastedRolls = 0;
            while (budget >= cheapestPrice && chosen.Count < maxItems)
            {
                if (wastedRolls >= MaxWastedRolls)
                {
                    Tracker.TrackError($"Loot rolls aborted after {MaxWastedRolls} wasted rolls, remaining budget: {budget}.", this);
                    break;
                }

                int chosenTier = MakeRoll(actualTierChances);
                if (chosenTier < 0 || chosenTier >= tierPrices.Length)
                {
                    wastedRolls++;
                    continue;
                }

                if (minPriceByTier.GetValueOrDefault(chosenTier, float.MaxValue) > budget)
                    chosenTier = FindClosestAffordableTier(chosenTier, budget, minPriceByTier, tierPrices.Length);
                if (chosenTier < 0)
                {
                    wastedRolls++;
                    continue;
                }

                var tableRecords = modifiedTable[chosenTier];
                var randomItem = tableRecords[_rnd.RandIntRange(0, tableRecords.Count - 1)];
                if (randomItem.Price > budget || string.IsNullOrWhiteSpace(randomItem.Id)) randomItem = PickRandomAffordableItem(tableRecords, budget);
                if (randomItem == null)
                {
                    wastedRolls++;
                    continue;
                }

                budget -= randomItem.Price;

                // Tier upgrade is a free quality swap AFTER the purchase: the roll already paid the
                // original tier's price — the rest was paid by the modifier's difficulty. Gating the
                // upgrade by budget would silently void the modifier on every mid-budget NPC.
                int upgradedTier = tryUpgradeTier(chosenTier, _rnd.RandFloat());
                if (upgradedTier >= 0 && upgradedTier < chosenTier && modifiedTable.TryGetValue(upgradedTier, out var upgradedRecords))
                {
                    var upgradedItem = PickRandomAffordableItem(upgradedRecords, float.MaxValue);
                    if (upgradedItem != null)
                    {
                        randomItem = upgradedItem;
                        chosenTier = upgradedTier;
                    }
                }

                tiersAmount[chosenTier]++;
                chosen.Add((randomItem, chosenTier));
            }

            ConvertLeftoverBudgetToQuality(budget, maxItems, chosen, tiersAmount, modifiedTable);

            _gameEventBus.Publish<ItemTierChosenEvent>(new(tiersAmount));

            return chosen.Select(entry => entry.Record.Id).ToList();
        }

        /// <summary>The item cap must not flatten the reward curve: when the cap stopped the spending,
        /// the leftover budget repeatedly swaps the cheapest chosen item for a better-tier one.</summary>
        private void ConvertLeftoverBudgetToQuality(
            float budget,
            int maxItems,
            List<(TableRecord Record, int Tier)> chosen,
            Dictionary<int, int> tiersAmount,
            Dictionary<int, List<TableRecord>> modifiedTable)
        {
            if (chosen.Count < maxItems || chosen.Count == 0) return;

            int guard = 0;
            while (budget > 0 && guard++ < MaxWastedRolls)
            {
                int cheapestIndex = 0;
                for (int i = 1; i < chosen.Count; i++)
                    if (chosen[i].Record.Price < chosen[cheapestIndex].Record.Price)
                        cheapestIndex = i;

                (TableRecord cheapest, int cheapestTier) = chosen[cheapestIndex];

                List<(TableRecord Record, int Tier)> candidates = [];
                for (int betterTier = cheapestTier - 1; betterTier >= 0; betterTier--)
                {
                    if (!modifiedTable.TryGetValue(betterTier, out var records)) continue;
                    candidates.AddRange(records
                        .Where(record => !string.IsNullOrWhiteSpace(record.Id) && record.Price > cheapest.Price && record.Price - cheapest.Price <= budget)
                        .Select(record => (record, betterTier)));
                }

                if (candidates.Count == 0) return;

                var (replacement, replacementTier) = candidates[_rnd.RandIntRange(0, candidates.Count - 1)];
                budget -= replacement.Price - cheapest.Price;
                tiersAmount[cheapestTier]--;
                tiersAmount[replacementTier]++;
                chosen[cheapestIndex] = (replacement, replacementTier);
            }
        }

        private List<ItemStack> GenerateChosenItems(float[] actualRarityChances, IModifierApplyingContext context, List<string> chosenItemsIds)
        {
            var items = new List<ItemStack>();
            var rarityAmount = Enum.GetValues<Rarity>().ToDictionary(x => x, _ => 0);
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

                // One bad id (a table entry without item data) must not swallow the rest of the drop —
                // especially the guaranteed items appended after the rolled ones.
                try
                {
                    Rarity rarity = context.TryUpgradeRarity((Rarity)MakeRoll(actualRarityChances));
                    // ItemModifierMultiplier is the stat gain per point of total difficulty: no modifiers → items at data values.
                    float modifierMultiplier = 1f + _configuration.ItemModifierMultiplier * context.TotalDifficultyMultiplier;
                    var item = _itemCreationService.CreateItem(id, context.AdditionalItemEffects, rarity, _configuration.EquipItemEffectChance, modifierMultiplier);
                    rarityAmount[item.Rarity]++;
                    items.Add(new ItemStack(item) { Stack = 1 });
                }
                catch (Exception ex)
                {
                    Tracker.TrackException($"Failed to create loot item '{id}'.", ex, this);
                }
            }

            _gameEventBus.Publish(new EquipRarityChosenEvent(rarityAmount));
            return items;
        }

        private int MakeRoll(float[] chances)
        {
            float roll = _rnd.RandFloat();
            float cumulative = 0f;

            for (int i = 0; i < chances.Length; i++)
            {
                cumulative += chances[i];
                if (roll < cumulative)
                    return i;
            }

            return chances.Length - 1;
        }

        private static Dictionary<int, float> BuildMinPriceByTier(Dictionary<int, List<TableRecord>> table) =>
            table.Where(kvp => kvp.Value.Any(record => !string.IsNullOrWhiteSpace(record.Id)))
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Where(record => !string.IsNullOrWhiteSpace(record.Id)).Min(record => record.Price));

        /// <summary>Falls to the nearest WORSE tier the budget can still afford; -1 when none can.</summary>
        private static int FindClosestAffordableTier(int fromTier, float budget, Dictionary<int, float> minPriceByTier, int tierCount)
        {
            for (int tier = fromTier + 1; tier < tierCount; tier++)
                if (minPriceByTier.GetValueOrDefault(tier, float.MaxValue) <= budget)
                    return tier;

            return -1;
        }

        // A uniform pick among the affordable records: a deterministic "best affordable" fallback would
        // funnel most small-budget kills into the single most expensive item of the tier.
        private TableRecord? PickRandomAffordableItem(List<TableRecord> tableRecords, float budget)
        {
            var affordable = tableRecords.Where(record => !string.IsNullOrWhiteSpace(record.Id) && record.Price <= budget).ToList();
            return affordable.Count == 0 ? null : affordable[_rnd.RandIntRange(0, affordable.Count - 1)];
        }

        private void OnBattleEnds(BattleEndEvent obj)
        {
            _tableCache.Clear();
            _diedEntities.Clear();
        }

        // Copies tier AND rarity chance arrays — size must follow the source, not the tier config.
        private static float[] CopyBaseChances(float[] baseChancesToCopy)
        {
            float[] chances = new float[baseChancesToCopy.Length];
            baseChancesToCopy.CopyTo(chances, 0);
            return chances;
        }

        private IModifierApplyingContext CreateModifierApplyingContext(IFightableNpc npc)
        {
            var modifierApplyingContext = new ModifierApplyingContext();
            foreach (INpcModifier npcNpcModifier in npc.NpcModifiers.AllModifiers)
                npcNpcModifier.ApplyModifier(modifierApplyingContext);
            modifierApplyingContext.TotalDifficultyMultiplier = npc.NpcModifiers.AllModifiers.Sum(mod => mod.DifficultyMultiplier);
            return modifierApplyingContext;
        }

        private float CalculateBudget(IFightableNpc npc)
        {
            float baseBudget = _configuration.BaseBudget.GetValueOrDefault(npc.EntityType, 1f);
            float rarityMultiplier = _configuration.RarityMultipliers.GetValueOrDefault(npc.Rarity, 1f);
            float difficultyMultiplier = npc.NpcModifiers.AllModifiers.Sum(mod => mod.DifficultyMultiplier);
            // TODO:
            // Хрупко. При изменении порядка EntityType или добавлении нового сломается
            float f = (int)npc.EntityType > 3 ? Mathf.Log(npc.Level + 1) : Mathf.Sqrt(npc.Level);
            return baseBudget * (1 + _configuration.LvlCoefficient * f) * rarityMultiplier * (1 + difficultyMultiplier);
        }
    }
}

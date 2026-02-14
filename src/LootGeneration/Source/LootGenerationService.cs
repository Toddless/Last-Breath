namespace LootGeneration.Source
{
    using Godot;
    using System;
    using Internal;
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
    using Core.Interfaces.MessageBus;
    using Core.Interfaces.Events.GameEvents;

    public class LootGenerationService : ILootGenerationService
    {
        private readonly RandomNumberGenerator _rnd = new();
        private readonly IItemDataProvider _dataProvider;
        private readonly IGameMessageBus _gameMessageBus;
        private readonly IItemEffectProvider _effectProvider;
        private readonly IGameEventBus _gameEventBus;
        private readonly Dictionary<string, Dictionary<int, List<TableRecord>>> _tableCache = [];
        private ILootConfiguration _configuration;

        public LootGenerationService(IItemDataProvider itemDataProvider, IGameEventBus eventBus, IGameMessageBus messageBus, ILootConfiguration configuration,
            IItemEffectProvider itemEffectProvider)
        {
            _configuration = configuration;
            _dataProvider = itemDataProvider;
            _gameEventBus = eventBus;
            _gameMessageBus = messageBus;
            _effectProvider = itemEffectProvider;
            _gameEventBus.Subscribe<BattleEndEvent>(OnBattleEnds);
            _rnd.Randomize();
        }

        public void ChangeLootConfiguration(ILootConfiguration configuration) => _configuration = configuration;

        public async Task<List<ItemStack>> GenerateItemsAsync(IEntity diedEntity)
        {
            if (diedEntity is not INpc npc) return [];

            ApplyNpcModifiers(npc, out IModifierApplyingContext context);

            CopyBaseTierChances(out float[] tierChances);

            float budget = CalculateBudget(npc);

            var npcModifiers = npc.NpcModifiers.AllModifiers;

            float[] actualTierChances = Calculations.CalculateChances<ITierMultiplierModifier>(npcModifiers, tierChances);

            // same npc can die multiple times per battle
            if (!_tableCache.TryGetValue(npc.InstanceId, out Dictionary<int, List<TableRecord>>? baseTable))
            {
                baseTable = await _gameMessageBus.Send<GetLootTableRequest, Dictionary<int, List<TableRecord>>>(new(npc.Fraction, npc.EntityType, npc.Id));
                _tableCache[npc.InstanceId] = baseTable;
            }

            var modifiedTable = new Dictionary<int, List<TableRecord>>(baseTable);
            foreach (var kvp in context.AdditionalItems)
            {
                if (modifiedTable.TryGetValue(kvp.Key, out var existingList)) existingList.AddRange(kvp.Value);
                else modifiedTable[kvp.Key] = [..kvp.Value];
            }

            Dictionary<int, int> tiersAmount = new() { [0] = 0, [1] = 0, [2] = 0, [3] = 0 };
            List<string> chosenItemsIds = [];
            int[] tierPrices = _configuration.TierPrices;
            while (budget > tierPrices[^1])
            {
                int chosenTier = MakeRoll(actualTierChances);
                chosenTier = context.TryUpgradeTier(chosenTier, _rnd.Randf());

                if (chosenTier < 0 || chosenTier >= tierPrices.Length) continue;
                if (tierPrices[chosenTier] > budget)
                {
                    chosenTier = FindFirstSuitableTier(budget);
                }

                if (!modifiedTable.TryGetValue(chosenTier, out var tableRecords)) continue;
                if (tableRecords.Count == 0) continue;

                int randomNumber = _rnd.RandiRange(0, tableRecords.Count - 1);
                var randomItem = tableRecords[randomNumber];
                if (randomItem.Price > budget) continue;
                if (string.IsNullOrWhiteSpace(randomItem.Id)) continue;
                tiersAmount[chosenTier]++;
                budget -= randomItem.Price;
                chosenItemsIds.Add(randomItem.Id);
            }

            _gameEventBus.Publish<ItemTierChosenEvent>(new(tiersAmount));

            chosenItemsIds.AddRange(context.GuaranteedItems);

            CopyBaseRarityChances(out float[] rarityChances);
            float[] actualRarityChances = Calculations.CalculateChances<IRarityUpgradeModifier>(npcModifiers, rarityChances);
            return GenerateChosenItems(actualRarityChances, context, chosenItemsIds);
        }

        private List<ItemStack> GenerateChosenItems(float[] actualRarityChances, IModifierApplyingContext context, List<string> chosenItemsIds)
        {
            var items = new List<ItemStack>();
            var rarityAmount = Enum.GetValues<Rarity>().ToDictionary(x => x, y => 0);
            try
            {
                _gameEventBus.Publish<ChosenItemIds>(new(chosenItemsIds.ToList()));
                foreach (string id in chosenItemsIds)
                {
                    if (string.IsNullOrWhiteSpace(id)) continue;

                    // looking for stackable item
                    var existingItemStack = items.FirstOrDefault(x => x.Item.Id == id && x.Item is not IEquipItem);

                    if (existingItemStack != null)
                    {
                        existingItemStack.Stack++;
                        rarityAmount[existingItemStack.Item.Rarity]++;
                        continue;
                    }

                    var item = _dataProvider.CopyBaseItem(id);
                    if (item is IEquipItem equip)
                        HandleEquipItemGeneration(equip, actualRarityChances, context);

                    rarityAmount[item.Rarity]++;
                    items.Add(new(item) { Stack = 1 });
                }

                GD.Print($"Items generated: {items.Sum(x => x.Stack)}");
                _gameEventBus.Publish<EquipRarityChosenEvent>(new(rarityAmount));
                return items;
            }
            catch (Exception ex)
            {
                Tracker.TrackException($"Failed to generate chosen items.", ex, this);
                return items;
            }
        }

        private void HandleEquipItemGeneration(IEquipItem equip, float[] actualRarityChances, IModifierApplyingContext context)
        {
            if (equip.Rarity is Rarity.Mythic or Rarity.Unique) return;

            Rarity rarityRoll = (Rarity)MakeRoll(actualRarityChances);
            rarityRoll = context.TryUpgradeRarity(rarityRoll);
            // concat all item effects with effects from context

            var allEffects = _effectProvider.GetCopyItemsEffects().Concat(context.AdditionalItemEffects).ToList();
            string equipItemEffect = _rnd.Randf() <= _configuration.EquipItemEffectChance
                ? allEffects[_rnd.RandiRange(0, allEffects.Count)]
                : string.Empty;
            equip.SetItemEffect(equipItemEffect);
            equip.Rarity = rarityRoll;
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

        private void OnBattleEnds(BattleEndEvent obj) => _tableCache.Clear();

        private void CopyBaseTierChances(out float[] tierChances)
        {
            tierChances = new float[_configuration.BaseTierChances.Length];
            _configuration.BaseTierChances.CopyTo(tierChances, 0);
        }

        private void CopyBaseRarityChances(out float[] rarityChances)
        {
            rarityChances = new float[_configuration.BaseRarityChances.Length];
            _configuration.BaseRarityChances.CopyTo(rarityChances, 0);
        }

        private void ApplyNpcModifiers(INpc npc, out IModifierApplyingContext modifierApplyingContext)
        {
            modifierApplyingContext = new ModifierApplyingContext();
            foreach (INpcModifier npcNpcModifier in npc.NpcModifiers.AllModifiers)
                npcNpcModifier.ApplyModifier(modifierApplyingContext);
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

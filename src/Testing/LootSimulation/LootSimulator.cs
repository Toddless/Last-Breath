namespace LastBreathTest.LootSimulation
{
    using Core;
    using Core.Entity;
    using Core.Enums;
    using Core.Events.GameEvents;
    using Core.Items;
    using LootGeneration.Source;

    /// <summary>Monte-Carlo runner: kills the same archetype N times through the real pipeline
    /// and aggregates the drops. A BattleEndEvent is published after every kill so the service
    /// caches behave exactly one-battle-per-kill.</summary>
    internal sealed class LootSimulator(LootPipeline pipeline)
    {
        public async Task<ScenarioResult> RunAsync(NpcArchetype archetype, int kills)
        {
            var combinedTable = await pipeline.GetCombinedTableAsync(archetype);
            var tierByItem = new Dictionary<string, int>();
            var priceByItem = new Dictionary<string, float>();
            foreach ((int tier, var records) in combinedTable)
                foreach (var record in records)
                {
                    // Dirty table entries (missing id) exist in data; the pipeline skips them the same way.
                    if (string.IsNullOrWhiteSpace(record.Id)) continue;
                    if (!tierByItem.TryGetValue(record.Id, out int existing) || tier < existing) tierByItem[record.Id] = tier;
                    priceByItem.TryAdd(record.Id, record.Price);
                }

            var killRecords = new List<KillRecord>(kills);
            var tierDistribution = new Dictionary<int, int>();
            var rarityDistribution = new Dictionary<Rarity, int>();
            var dropFrequency = new Dictionary<string, int>();

            for (int i = 0; i < kills; i++)
            {
                var modifiers = ResolveModifiers(archetype);
                var guaranteedIds = modifiers.OfType<IGuaranteedItemsModifier>().SelectMany(modifier => modifier.Items).ToHashSet();
                float difficulty = modifiers.Sum(modifier => modifier.DifficultyMultiplier);

                var npc = SimNpc.Create(archetype, modifiers);
                var stacks = await pipeline.LootService.GenerateItemsAsync(npc);

                var drops = new List<DropRecord>(stacks.Count);
                foreach (var stack in stacks)
                {
                    var drop = new DropRecord(
                        stack.Item.Id,
                        stack.Item.Rarity,
                        stack.Stack,
                        tierByItem.GetValueOrDefault(stack.Item.Id, -1),
                        priceByItem.GetValueOrDefault(stack.Item.Id, 0f),
                        guaranteedIds.Contains(stack.Item.Id),
                        stack.Item is IEquipItem);
                    drops.Add(drop);

                    rarityDistribution[drop.Rarity] = rarityDistribution.GetValueOrDefault(drop.Rarity) + drop.Stack;
                    dropFrequency[drop.ItemId] = dropFrequency.GetValueOrDefault(drop.ItemId) + drop.Stack;
                }

                var tierEvent = pipeline.Events.TakeLast<ItemTierChosenEvent>();
                if (tierEvent != null)
                    foreach ((int tier, int amount) in tierEvent.ChosenTiersAmount)
                        tierDistribution[tier] = tierDistribution.GetValueOrDefault(tier) + amount;

                killRecords.Add(new KillRecord(ExpectedBudget(archetype, difficulty), difficulty, drops));

                // Flush the per-battle caches (_tableCache/_diedEntities) exactly as a won battle would.
                pipeline.Events.Publish(new BattleEndEvent(BattleResults.PlayerWon));
            }

            var orphans = tierByItem.Keys.Where(id => !dropFrequency.ContainsKey(id)).OrderBy(id => id).ToList();

            return new ScenarioResult(archetype, kills, killRecords, tierDistribution, rarityDistribution, dropFrequency, orphans);
        }

        /// <summary>Mirrors LootGenerationService.CalculateBudget on purpose: if the production formula
        /// drifts away from this expectation, the budget-conservation test fails and flags the change.</summary>
        public float ExpectedBudget(NpcArchetype archetype, float totalDifficulty)
        {
            var configuration = pipeline.Configuration;
            float baseBudget = configuration.BaseBudget.GetValueOrDefault(archetype.EntityType, 1f);
            float rarityMultiplier = configuration.RarityMultipliers.GetValueOrDefault(archetype.Rarity, 1f);
            float f = (int)archetype.EntityType > 3 ? MathF.Log(archetype.Level + 1) : MathF.Sqrt(archetype.Level);
            return baseBudget * (1 + configuration.LvlCoefficient * f) * rarityMultiplier * (1 + totalDifficulty);
        }

        private List<INpcModifier> ResolveModifiers(NpcArchetype archetype)
        {
            var modifiers = archetype.ModifierIds.Select(pipeline.ModifierProvider.GetModifier).ToList();
            if (archetype.RandomModifierCount <= 0) return modifiers;

            // Weighted sampling with replacement — the same roll the game spawner makes.
            (var weighted, float totalWeight) = WeightedRandomPicker.CalculateWeights(pipeline.ModifierProvider.GetAllModifiers());
            for (int i = 0; i < archetype.RandomModifierCount; i++)
                modifiers.Add(WeightedRandomPicker.PickRandom(weighted, totalWeight, pipeline.Rnd).Copy());

            return modifiers;
        }
    }
}

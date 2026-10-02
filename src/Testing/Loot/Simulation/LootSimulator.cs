namespace LastBreathTest.Loot.Simulation
{
    using Core.Data.NpcData;
    using Core.Entity;
    using Core.Enums;
    using Core.Events;
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
                        stack.Item is IEquipItem,
                        stack.Item is Core.Items.ICurrencyItem);
                    drops.Add(drop);

                    // The gold pile stays out of the item aggregates: a 20-coin stack would drown
                    // the Common share and the drop frequencies while not being an item at all.
                    if (drop.IsCurrency) continue;
                    rarityDistribution[drop.Rarity] = rarityDistribution.GetValueOrDefault(drop.Rarity) + drop.Stack;
                    dropFrequency[drop.ItemId] = dropFrequency.GetValueOrDefault(drop.ItemId) + drop.Stack;
                }

                var tierEvent = pipeline.Events.TakeLast<ItemTierChosenEvent>();
                if (tierEvent != null)
                    foreach ((int tier, int amount) in tierEvent.ChosenTiersAmount)
                        tierDistribution[tier] = tierDistribution.GetValueOrDefault(tier) + amount;

                killRecords.Add(new KillRecord(ExpectedBudget(archetype, difficulty), difficulty, modifiers.Count, drops));

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

        /// <summary>Mirror of NpcProvider.RollModifiers (issue #223), which the game spawner runs.
        /// Kept line-for-line equivalent on purpose: an authored count is an absolute and rolls no
        /// dice on the count; a cascade spawn walks the type × rarity slot ceiling and offers each
        /// slot to the falling chance of the NpcSpawnRolls catalog, stopping at the first refusal.
        /// Either way the composition is a weighted pick WITHOUT replacement over the id-ordered
        /// catalog — the pre-#223 sim sampled WITH replacement, which the game never did after the
        /// rework. If NpcProvider grows a seam this mirror cannot follow, stop and report it.</summary>
        private List<INpcModifier> ResolveModifiers(NpcArchetype archetype)
        {
            var modifiers = archetype.ModifierIds.Select(pipeline.ModifierProvider.GetModifier).ToList();

            int? authoredCount = archetype.RandomModifierCount > 0 ? archetype.RandomModifierCount : null;
            if (authoredCount == null && !archetype.CascadeRolled) return modifiers;

            int slots = authoredCount ?? NpcTypeDefaults.ModifierCount(archetype.EntityType, archetype.Rarity);
            var pool = pipeline.ModifierProvider.GetAllModifiers().ToList();

            List<INpcModifier> rolled = [];
            while (rolled.Count < slots && pool.Count > 0)
            {
                if (authoredCount == null && !FillsSlot(pipeline.SpawnRolls.ModifierSlotChance(archetype.EntityType, archetype.Rarity, rolled.Count))) break;

                long index = pipeline.Rnd.RandWeighted(pool.Select(modifier => modifier.Weight).ToArray());
                var picked = pool[Math.Max(0, (int)index)];
                pool.Remove(picked);
                rolled.Add(pipeline.ModifierProvider.GetModifier(picked.Id));
            }

            modifiers.AddRange(rolled);
            return modifiers;
        }

        /// <summary>Same certainty rule as NpcProvider.FillsSlot: a chance of 1 (or 0) is decided,
        /// not rolled, and spends no dice — part of the seeded-stream contract of the catalog.</summary>
        private bool FillsSlot(float chance) => chance >= 1f || (chance > 0f && pipeline.Rnd.RandFloat() < chance);
    }
}

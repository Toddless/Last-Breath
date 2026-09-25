namespace LastBreathTest.LootSimulation
{
    using Core.Enums;

    internal sealed record DropRecord(string ItemId, Rarity Rarity, int Stack, int Tier, float Price, bool IsGuaranteed, bool IsEquip, bool IsCurrency = false);

    internal sealed record KillRecord(float ExpectedBudget, float Difficulty, int ModifierCount, IReadOnlyList<DropRecord> Drops)
    {
        /// <summary>Real items only — the gold pile is budget leftover, not an item.</summary>
        public int ItemCount => Drops.Where(drop => !drop.IsCurrency).Sum(drop => drop.Stack);

        public float TotalValue => Drops.Where(drop => !drop.IsCurrency).Sum(drop => drop.Price * drop.Stack);

        /// <summary>Value bought with the budget — guaranteed items are free extras on top.</summary>
        public float SpentValue => Drops.Where(drop => drop is { IsGuaranteed: false, IsCurrency: false }).Sum(drop => drop.Price * drop.Stack);

        /// <summary>Gold minted from the kill's leftover budget (the coefficient's tuning input).</summary>
        public int GoldDropped => Drops.Where(drop => drop.IsCurrency).Sum(drop => drop.Stack);
    }

    internal sealed record ScenarioResult(
        NpcArchetype Archetype,
        int Kills,
        IReadOnlyList<KillRecord> KillRecords,
        IReadOnlyDictionary<int, int> TierDistribution,
        IReadOnlyDictionary<Rarity, int> RarityDistribution,
        IReadOnlyDictionary<string, int> DropFrequency,
        IReadOnlyList<string> OrphanItemIds)
    {
        public float MeanItemsPerKill => (float)KillRecords.Average(kill => kill.ItemCount);

        public float MeanValuePerKill => KillRecords.Average(kill => kill.TotalValue);

        public float MeanExpectedBudget => KillRecords.Average(kill => kill.ExpectedBudget);

        public float MeanDifficulty => KillRecords.Average(kill => kill.Difficulty);

        /// <summary>Average modifiers actually worn per kill — the #223 cascade's direct output.</summary>
        public float MeanModifierCount => (float)KillRecords.Average(kill => kill.ModifierCount);

        /// <summary>Average gold per kill — the goldPerBudgetUnit coefficient is tuned against this.</summary>
        public float MeanGoldPerKill => (float)KillRecords.Average(kill => kill.GoldDropped);

        /// <summary>Value gained per point of NPC difficulty — the reward-inflation curve input.</summary>
        public float ValuePerDifficultyPoint => MeanDifficulty > 0 ? MeanValuePerKill / MeanDifficulty : MeanValuePerKill;

        public float Percentile(Func<KillRecord, float> metric, float percentile)
        {
            float[] values = KillRecords.Select(metric).OrderBy(value => value).ToArray();
            if (values.Length == 0) return 0f;
            int index = Math.Clamp((int)MathF.Ceiling(percentile / 100f * values.Length) - 1, 0, values.Length - 1);
            return values[index];
        }
    }
}

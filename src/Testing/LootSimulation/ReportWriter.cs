namespace LastBreathTest.LootSimulation
{
    using System.Globalization;
    using System.Text;

    /// <summary>Renders scenario results into one markdown report — the balancing artifact.</summary>
    internal static class ReportWriter
    {
        public static string ToMarkdown(IReadOnlyList<ScenarioResult> results, int seed)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Loot simulation report");
            sb.AppendLine();
            // What the run WAS, never when it happened: a wall clock in the header makes every
            // regeneration differ and turns a byte comparison of two reports into noise.
            sb.AppendLine($"Seed: `{seed}`, scenarios: {results.Count}");
            sb.AppendLine();

            sb.AppendLine("## Summary");
            sb.AppendLine();
            sb.AppendLine("| Scenario | Kills | Mods/kill | Difficulty | Items/kill | Value/kill | p95 value | Budget | Value/difficulty | Gold/kill |");
            sb.AppendLine("|---|---|---|---|---|---|---|---|---|---|");
            foreach (var result in results)
            {
                sb.AppendLine(
                    $"| {result.Archetype.Name} | {result.Kills} | {F(result.MeanModifierCount)} | {F(result.MeanDifficulty)} | {F(result.MeanItemsPerKill)} " +
                    $"| {F(result.MeanValuePerKill)} | {F(result.Percentile(kill => kill.TotalValue, 95))} " +
                    $"| {F(result.MeanExpectedBudget)} | {F(result.ValuePerDifficultyPoint)} | {F(result.MeanGoldPerKill)} |");
            }

            foreach (var result in results) AppendScenario(sb, result);
            return sb.ToString();
        }

        private static void AppendScenario(StringBuilder sb, ScenarioResult result)
        {
            sb.AppendLine();
            sb.AppendLine($"## {result.Archetype.Name}");
            sb.AppendLine();
            sb.AppendLine($"{result.Archetype.EntityType}/{result.Archetype.Rarity}/lvl {result.Archetype.Level}, " +
                          $"modifiers: [{string.Join(", ", result.Archetype.ModifierIds)}]" +
                          (result.Archetype.RandomModifierCount > 0 ? $" + {result.Archetype.RandomModifierCount} rolled (authored count)" : string.Empty) +
                          (result.Archetype.CascadeRolled ? $" + spawn cascade (mean {F(result.MeanModifierCount)} of the ceiling)" : string.Empty));
            sb.AppendLine();

            int tierTotal = Math.Max(1, result.TierDistribution.Values.Sum());
            sb.AppendLine("| Tier | Rolls | Share |");
            sb.AppendLine("|---|---|---|");
            foreach ((int tier, int amount) in result.TierDistribution.OrderBy(kvp => kvp.Key))
                sb.AppendLine($"| {tier} | {amount} | {F(100f * amount / tierTotal)}% |");
            sb.AppendLine();

            int rarityTotal = Math.Max(1, result.RarityDistribution.Values.Sum());
            sb.AppendLine("| Rarity | Items | Share |");
            sb.AppendLine("|---|---|---|");
            foreach ((var rarity, int amount) in result.RarityDistribution.OrderBy(kvp => (int)kvp.Key))
                sb.AppendLine($"| {rarity} | {amount} | {F(100f * amount / rarityTotal)}% |");
            sb.AppendLine();

            sb.AppendLine("Top drops:");
            foreach ((string id, int amount) in result.DropFrequency.OrderByDescending(kvp => kvp.Value).Take(15))
                sb.AppendLine($"- {id}: {amount}");
            sb.AppendLine();

            sb.AppendLine(result.OrphanItemIds.Count == 0
                ? "No orphan items — every table entry dropped at least once."
                : $"Orphans ({result.OrphanItemIds.Count}): {string.Join(", ", result.OrphanItemIds)}");
        }

        private static string F(float value) => value.ToString("0.##", CultureInfo.InvariantCulture);
    }
}

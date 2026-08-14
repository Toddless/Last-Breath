namespace LastBreathTest.BattleSystemTests
{
    using Core.Data.AbilityData;
    using Newtonsoft.Json.Linq;

    /// <summary>Every position of the shipped loot tables must resolve to something the game can
    /// actually mint — a position naming an id needs a catalog entry, a position naming a set of
    /// augments needs at least one augment answering its filter. Either way an unresolvable position
    /// is a hard failure: the drop pipeline would burn budget on a seat that produces nothing.</summary>
    [TestClass]
    public class LootTablesAuditTests
    {
        /// <summary>The key a loot table position writes its augment filter under.</summary>
        private const string GroupProperty = "augments";

        /// <summary>The section of the ability catalog the augment records live in.</summary>
        private const string AugmentSection = "augments";

        [TestMethod]
        public void EveryLootTableIdExistsInTheCatalogs()
        {
            var lootIds = Positions()
                .Select(position => (string?)position["id"])
                .Where(id => !string.IsNullOrEmpty(id))
                .Select(id => id!)
                .ToHashSet();
            Assert.IsTrue(lootIds.Count > 0, "No ids found in the loot tables — the audit has nothing to audit.");

            var knownIds = CatalogIds("EquipItems", "items")
                .Concat(CatalogIds("Items", "items"))
                .Concat(CatalogIds("Recipes", "craftingRecipes"))
                .Concat(CatalogIds("Resources", "upgradeResources"))
                .Concat(CatalogIds("Resources", "craftingResources"))
                .ToHashSet();

            var orphans = lootIds.Where(id => !knownIds.Contains(id)).OrderBy(id => id).ToList();
            Assert.AreEqual(0, orphans.Count, $"Loot table ids without a catalog entry: {string.Join(", ", orphans)}");
        }

        /// <summary>A position names exactly one of the two kinds of drop. Naming both leaves what
        /// drops undecided, naming neither prices a seat that describes nothing — the parser refuses
        /// either, so a shipped table written that way would lose the line silently.</summary>
        [TestMethod]
        public void EveryLootTablePositionNamesOneKindOfDrop()
        {
            var confused = Positions()
                .Where(position => !string.IsNullOrEmpty((string?)position["id"]) == (position[GroupProperty] != null))
                .Select(position => position.ToString(Newtonsoft.Json.Formatting.None))
                .ToList();

            Assert.AreEqual(0, confused.Count, $"Loot table positions naming both an id and a group, or neither: {string.Join(", ", confused)}");
        }

        /// <summary>A group is expanded at mint time, so a filter no augment answers is not a load
        /// error at all — it is a kill that quietly drops one item fewer, forever. Membership is
        /// COVERAGE, not equality: a record declares a band and belongs to every seat inside it.</summary>
        [TestMethod]
        public void EveryLootTableGroupIsAnsweredByAShippedAugment()
        {
            var bands = AugmentBands();
            var unanswered = Positions()
                .Select(position => position[GroupProperty])
                .OfType<JObject>()
                .Select(group => ((int?)group["tier"], Rarity: Parse((string?)group["rarity"] ?? DefaultRarity)))
                .Where(filter => !bands.Any(band => band.Tier == filter.Item1 && Covers(band, filter.Rarity)))
                .Select(filter => $"tier {filter.Item1?.ToString() ?? "(none)"} / {filter.Rarity}")
                .Distinct()
                .ToList();

            Assert.AreEqual(0, unanswered.Count, $"Loot table groups no shipped augment answers: {string.Join(", ", unanswered)}");
        }

        /// <summary>An augment record leaving its rarity unstated is the plainest augment there is —
        /// the same reading <see cref="AbilityAugmentData"/> gives it.</summary>
        private const string DefaultRarity = nameof(Core.Enums.Rarity.Common);

        /// <summary>The scale runs downward — Legendary is zero — so the best end is the smaller
        /// number and a band contains everything between the two.</summary>
        private static bool Covers((int? Tier, Core.Enums.Rarity Worst, Core.Enums.Rarity Best) band, Core.Enums.Rarity rarity) =>
            (int)band.Best <= (int)rarity && (int)rarity <= (int)band.Worst;

        /// <summary>The tier and rarity band of every shipped augment record. A record naming no band
        /// is a band of one around the field it does name.</summary>
        private static List<(int? Tier, Core.Enums.Rarity Worst, Core.Enums.Rarity Best)> AugmentBands() =>
            [.. CatalogRoots("Abilities")
                .SelectMany(root => root[AugmentSection] as JArray ?? [])
                .Select(augment => (
                    (int?)augment["tier"],
                    Parse((string?)augment["minRarity"] ?? (string?)augment["rarity"] ?? DefaultRarity),
                    Parse((string?)augment["maxRarity"] ?? (string?)augment["rarity"] ?? DefaultRarity)))];

        private static Core.Enums.Rarity Parse(string rarity)
        {
            Assert.IsTrue(Enum.TryParse(rarity, out Core.Enums.Rarity parsed), $"'{rarity}' is not a rarity the game knows");
            return parsed;
        }

        private static IEnumerable<JObject> Positions() =>
            CatalogRoots("LootTables").SelectMany(root => root.SelectTokens("$..items[*]")).OfType<JObject>();

        private static IEnumerable<string> CatalogIds(string catalog, string arrayProperty) =>
            CatalogRoots(catalog)
                .SelectMany(root => root[arrayProperty] as JArray ?? [])
                .Select(token => (string?)token["id"])
                .Where(id => !string.IsNullOrEmpty(id))
                .Select(id => id!);

        private static IEnumerable<JObject> CatalogRoots(string catalog)
        {
            string path = Path.Combine(SharedDataRoot(), catalog);
            if (!Directory.Exists(path)) yield break;
            foreach (string file in Directory.EnumerateFiles(path, "*.json", SearchOption.AllDirectories))
                yield return JObject.Parse(File.ReadAllText(file));
        }

        private static string SharedDataRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "SharedData")))
                directory = directory.Parent;
            Assert.IsNotNull(directory, "SharedData not found above the test bin directory");
            return Path.Combine(directory.FullName, "SharedData");
        }
    }
}

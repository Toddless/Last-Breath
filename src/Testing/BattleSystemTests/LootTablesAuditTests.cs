namespace LastBreathTest.BattleSystemTests
{
    using Newtonsoft.Json.Linq;

    /// <summary>Every id referenced by the loot tables must resolve to a shipped catalog entry —
    /// an unknown id is a hard failure (the drop pipeline would burn budget on an uncreatable item).</summary>
    [TestClass]
    public class LootTablesAuditTests
    {
        [TestMethod]
        public void EveryLootTableIdExistsInTheCatalogs()
        {
            var lootIds = CatalogRoots("LootTables")
                .SelectMany(root => root.SelectTokens("$..items[*].id"))
                .Select(token => (string?)token)
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

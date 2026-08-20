namespace LastBreathTest.BattleSystemTests
{
    using System.Text;
    using Core.Localization;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// The localization audit: keys required by SharedData ids vs en.po, and en.po vs ru.po.
    /// Reports gaps to the test output (Inconclusive, never red); the only hard failure is a
    /// duplicate msgid — that corrupts the .po.
    /// </summary>
    [TestClass]
    public class LocalizationAuditTests
    {
        private static string SrcRoot
        {
            get
            {
                var dir = new DirectoryInfo(AppContext.BaseDirectory);
                while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "SharedData")))
                    dir = dir.Parent;
                Assert.IsNotNull(dir, "SharedData not found above test bin directory");
                return dir.FullName;
            }
        }

        private static string Shared => Path.Combine(SrcRoot, "SharedData");

        [TestMethod]
        public void PoFilesHaveNoDuplicateKeys()
        {
            foreach (string po in new[] { "en.po", "ru.po" })
            {
                var duplicates = ReadMsgIds(po).GroupBy(id => id).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
                Assert.AreEqual(0, duplicates.Count, $"{po} duplicate msgids: {string.Join(", ", duplicates)}");
            }
        }

        [TestMethod]
        public void ReportMissingKeys()
        {
            var en = ReadMsgIds("en.po").ToHashSet();
            var ru = ReadMsgIds("ru.po").ToHashSet();
            var report = new StringBuilder();
            int missingTotal = 0;

            foreach ((string domain, List<string> ids, bool needsDescription) in CollectDataIds())
            {
                var missingNames = ids.Where(id => !en.Contains(id)).ToList();
                var missingDescriptions = needsDescription ? ids.Where(id => !en.Contains(id + "_Description")).ToList() : [];
                missingTotal += missingNames.Count + missingDescriptions.Count;

                if (missingNames.Count > 0)
                    report.AppendLine($"[{domain}] нет имён в en.po ({missingNames.Count}):\n  {string.Join("\n  ", missingNames)}");
                if (missingDescriptions.Count > 0)
                    report.AppendLine($"[{domain}] нет описаний в en.po ({missingDescriptions.Count}):\n  {string.Join("\n  ", missingDescriptions.Select(id => id + "_Description"))}");
            }

            var missingInRu = en.Where(id => !ru.Contains(id)).OrderBy(id => id).ToList();
            if (missingInRu.Count > 0)
                report.AppendLine($"[ru.po] ключей нет вовсе ({missingInRu.Count}):\n  {string.Join("\n  ", missingInRu)}");

            Console.WriteLine(report.Length == 0 ? "Локализация полна." : report.ToString());
            if (missingTotal + missingInRu.Count > 0)
                Assert.Inconclusive($"en.po: не хватает {missingTotal} ключей из данных; ru.po: отстаёт на {missingInRu.Count} ключей. Полный список — в output теста.");
        }

        /// <summary>
        /// The augment tooltip's tier line, both halves of it. A key with no wording prints itself, and a
        /// wording whose placeholder nobody fills prints the placeholder — the tier reaches the player only
        /// when the catalog words the key under the name <see cref="Battle.Source.UIElements.AugmentTrayTile"/>
        /// puts in its values.
        /// </summary>
        [TestMethod]
        public void AugmentTierLineIsWordedAndFilledByTheTilePlaceholder()
        {
            const string key = "UI_Augment_Tier";
            const string tilePlaceholder = "Value";

            string? template = ReadEntries("en.po").GetValueOrDefault(key);
            Assert.IsFalse(string.IsNullOrEmpty(template), $"en.po words no '{key}' — the tooltip shows the raw key");

            var provider = new FakeLocalizationProvider();
            string line = new TextTemplateEngine(provider)
                .Render(template!, new Dictionary<string, object?> { [tilePlaceholder] = 2 }, TextFormat.Plain);

            StringAssert.Contains(line, "2", $"'{key}' never places the tier — it names no '{{{tilePlaceholder}}}'");
            Assert.IsFalse(line.Contains('{'), $"'{key}' keeps a placeholder the tile does not fill: {line}");
        }

        private static List<(string Domain, List<string> Ids, bool NeedsDescription)> CollectDataIds() =>
        [
            // Modifier and ParameterChange templates localize parameter names by enum member
            ("EntityParameter", [.. Enum.GetNames<Core.Enums.EntityParameter>()], false),
            ("EquipItems", CatalogIds("EquipItems", "items"), true),
            ("Items", CatalogIds("Items", "items"), true),
            ("Recipes", CatalogIds("Recipes", "craftingRecipes"), false),
            ("Resources", [.. CatalogIds("Resources", "upgradeResources"), .. CatalogIds("Resources", "craftingResources")], false),
            ("Abilities", CatalogIds("Abilities", "abilities"), true),
            // Augments are records of their own in the same catalog, not a nested list under an ability
            ("Augments", CatalogIds("Abilities", "augments"), true),
            ("Npc", CatalogIds("Npc", "npcs"), false),
            ("NpcModifiers", NestedIds("NpcModifiers", "mods", "modifiers"), true),
            // A condition is worded under a key derived from its id, not under the id itself
            ("Conditions", CatalogIds("Conditions", "conditions").Select(Core.Localization.ConditionalLineText.ClauseKey).ToList(), false),
        ];

        private static List<string> CatalogIds(string catalog, string arrayProperty) =>
            CatalogRoots(catalog)
                .SelectMany(root => root[arrayProperty] as JArray ?? [])
                .Select(token => (string?)token["id"])
                .Where(id => !string.IsNullOrEmpty(id))
                .Select(id => id!)
                .Distinct()
                .ToList();

        private static List<string> NestedIds(string catalog, string outerProperty, string innerProperty) =>
            CatalogRoots(catalog)
                .SelectMany(root => root[outerProperty] as JArray ?? [])
                .SelectMany(outer => outer[innerProperty] as JArray ?? [])
                .Select(token => (string?)token["id"])
                .Where(id => !string.IsNullOrEmpty(id))
                .Select(id => id!)
                .Distinct()
                .ToList();

        private static IEnumerable<JObject> CatalogRoots(string catalog)
        {
            string path = Path.Combine(Shared, catalog);
            if (!Directory.Exists(path)) yield break;
            foreach (string file in Directory.EnumerateFiles(path, "*.json", SearchOption.AllDirectories))
                yield return JObject.Parse(File.ReadAllText(file));
        }

        /// <summary>Key → wording for the one-line entries the catalog is written in; a continued
        /// entry simply comes back with its first line, which is enough to tell a worded key from an
        /// empty one.</summary>
        private static Dictionary<string, string> ReadEntries(string poFileName)
        {
            string[] lines = File.ReadAllLines(Path.Combine(Shared, "Localization", poFileName));
            var entries = new Dictionary<string, string>(StringComparer.Ordinal);

            for (int i = 0; i + 1 < lines.Length; i++)
            {
                if (!Quoted(lines[i], "msgid ", out string id) || id.Length == 0) continue;
                if (Quoted(lines[i + 1], "msgstr ", out string text)) entries[id] = text;
            }

            return entries;
        }

        private static bool Quoted(string line, string prefix, out string value)
        {
            value = string.Empty;
            if (!line.StartsWith(prefix + '"', StringComparison.Ordinal) || !line.EndsWith('"')) return false;

            value = line[(prefix.Length + 1)..^1];
            return true;
        }

        private static List<string> ReadMsgIds(string poFileName)
        {
            string path = Path.Combine(Shared, "Localization", poFileName);
            return File.ReadAllLines(path)
                .Where(line => line.StartsWith("msgid \"", StringComparison.Ordinal))
                .Select(line => line["msgid \"".Length..^1])
                .Where(id => id.Length > 0)
                .ToList();
        }
    }
}

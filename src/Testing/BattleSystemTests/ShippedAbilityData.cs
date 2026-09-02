namespace LastBreathTest.BattleSystemTests
{
    using Battle.Source;
    using Battle.Source.Abilities;
    using Core.Battle.Abilities;
    using Core.Data.GameData;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// The shipped ability data as the game reads it: the real source, the real loader, the real
    /// parsers. The catalog is what reads that file — the records of both sections are Core's, because
    /// every composition needs them — while the registry that BUILDS an ability or an augment out of a
    /// record is the battle module's and reads nothing. A test asking about either takes both out of a
    /// single load, the way a composed game holds them.
    /// </summary>
    internal static class ShippedAbilityData
    {
        /// <summary>The abilities the shipped files declare, already able to offer augments.</summary>
        internal static AbilityProvider Abilities() => Load().Abilities;

        /// <summary>The augment records the shipped files declare.</summary>
        internal static AbilityAugmentCatalog Augments() => Load().Augments;

        internal static (AbilityProvider Abilities, AbilityAugmentCatalog Augments) Load() => LoadFrom(SharedData.Root());

        /// <summary>The augment records of a catalog a test wrote, read through the same loader the
        /// game runs — the fitting rule reads fields no hand-built record can prove are ever parsed,
        /// and a case that writes its own pair keeps that pair whatever the shipped markup becomes.</summary>
        internal static AbilityAugmentCatalog CatalogOver(string json)
        {
            string root = Directory.CreateTempSubdirectory("augments_").FullName;
            Directory.CreateDirectory(Path.Combine(root, DataCatalog.Abilities));
            File.WriteAllText(Path.Combine(root, DataCatalog.Abilities, "Abilities.json"), json);
            CopyCatalog(DataCatalog.Effects, root);

            AbilityAugmentCatalog catalog = LoadFrom(root).Augments;
            Directory.Delete(root, recursive: true);
            return catalog;
        }

        /// <summary>The ability registry over a catalog a test wrote, through the real loader — for
        /// walks that need to build the shipped abilities from DOCTORED data.</summary>
        internal static AbilityProvider AbilitiesOver(string json)
        {
            string root = Directory.CreateTempSubdirectory("abilities_").FullName;
            Directory.CreateDirectory(Path.Combine(root, DataCatalog.Abilities));
            File.WriteAllText(Path.Combine(root, DataCatalog.Abilities, "Abilities.json"), json);
            CopyCatalog(DataCatalog.Effects, root);

            AbilityProvider abilities = LoadFrom(root).Abilities;
            Directory.Delete(root, recursive: true);
            return abilities;
        }

        /// <summary>json camelCase to the PascalCase parameter name, the way the base registers it.</summary>
        internal static string ParameterKey(string property) => char.ToUpperInvariant(property[0]) + property[1..];

        /// <summary>Every shipped ability whose record carries property keys, and the keys it carries.</summary>
        internal static IEnumerable<(string AbilityId, List<string> Keys)> ShippedProperties()
        {
            foreach (JObject entry in AbilityEntries())
            {
                List<string> keys = entry["abilityProperties"] is JObject properties
                    ? [.. properties.Properties().Select(property => property.Name)]
                    : [];
                if (keys.Count > 0) yield return ((string?)entry["id"] ?? string.Empty, keys);
            }
        }

        /// <summary>The shipped catalog with every ability's properties emptied — what each ability
        /// declares on its own, with nothing the data added.</summary>
        internal static string WithoutProperties()
        {
            JObject root = AbilityCatalog();
            foreach (JObject entry in (root["abilities"] as JArray ?? []).OfType<JObject>())
                entry["abilityProperties"] = new JObject();

            return root.ToString();
        }

        private static IEnumerable<JObject> AbilityEntries() =>
            (AbilityCatalog()["abilities"] as JArray ?? []).OfType<JObject>();

        /// <summary>The shipped ability catalog as markup, read whole — for the walks that DOCTOR a
        /// record and hand the result back to the loader.</summary>
        internal static JObject AbilityCatalog()
        {
            string[] files = [.. Directory.EnumerateFiles(SharedData.Catalog(DataCatalog.Abilities), "*.json", SearchOption.AllDirectories)];
            Assert.AreEqual(1, files.Length, "the ability catalog is no longer one file — this walk rewrites it whole");
            return JObject.Parse(File.ReadAllText(files[0]));
        }

        /// <summary>Puts a shipped catalog beside a catalog a test wrote. The loader reads every catalog
        /// its participants declare, and a root missing one fails the load rather than skipping it.</summary>
        private static void CopyCatalog(string catalog, string root)
        {
            string source = SharedData.Catalog(catalog);
            string destination = Path.Combine(root, catalog);
            Directory.CreateDirectory(destination);
            foreach (string file in Directory.EnumerateFiles(source, "*.json"))
                File.Copy(file, Path.Combine(destination, Path.GetFileName(file)));
        }

        /// <summary>The same pair over any data root — for records the shipped files do not declare.</summary>
        internal static (AbilityProvider Abilities, AbilityAugmentCatalog Augments) LoadFrom(string root)
        {
            (AbilityProvider abilities, AbilityAugmentCatalog augments, _) = ComposeFrom(root);
            return (abilities, augments);
        }

        /// <summary>The shipped data with the effect canon beside it — for walks that need the registry
        /// itself and not only the records that read through it.</summary>
        internal static (AbilityProvider Abilities, AbilityAugmentCatalog Augments, EffectProvider Effects) Composed() =>
            ComposeFrom(SharedData.Root());

        private static (AbilityProvider Abilities, AbilityAugmentCatalog Augments, EffectProvider Effects) ComposeFrom(string root)
        {
            var augments = new AbilityAugmentCatalog();
            // The effect registry reads the canonical numbers as a participant of this very load, so a
            // record reaching an effect through this stand travels the road it travels in the game:
            // the canon supplies what the record leaves unsaid. An unloaded registry here would leave
            // every such walk asserting about numbers no shipped composition ever uses.
            var effects = new EffectProvider();
            var abilities = new AbilityProvider(augments, () => effects);
            var service = new GameDataService(new FileSystemDataSource(root), [augments, effects]);
            List<string> failures = [];
            service.LoadFailed += (context, exception) => failures.Add($"{context}: {exception.Message}");

            service.LoadAll();

            Assert.AreEqual(0, failures.Count, string.Join("; ", failures));
            return (abilities, augments, effects);
        }
    }
}

namespace LastBreathTest.Augment
{
    using Ability;
    using Battle.Source.Abilities;
    using Core.Battle.Abilities;
    using Core.Data.GameData;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// One list written twice. An augment exists as a record in the data and as a factory in the
    /// registry, and neither half means anything alone: a record no factory answers parses, is
    /// offered to the player and then quietly builds nothing, while a factory no record names is
    /// code nothing can ever reach. The two were kept in step by hand until now, which is a
    /// bookkeeping habit and not a guarantee — the walk below makes the two sets one another's
    /// definition, in both directions, so a record added without its factory (or the other way
    /// round) fails here instead of in a build.
    /// </summary>
    [TestClass]
    public class AugmentRegistryCoverageTests
    {
        [TestMethod]
        public void EveryDeclaredAugmentHasAFactoryToBuildIt()
        {
            (AbilityProvider registryHolder, AbilityAugmentCatalog catalog) = ShippedAbilityData.Load();
            var registry = registryHolder.BuildableAugmentIds.ToHashSet(StringComparer.Ordinal);

            var unbuildable = DeclaredIds(catalog).Where(id => !registry.Contains(id)).OrderBy(id => id).ToList();

            Assert.AreEqual(0, unbuildable.Count,
                $"declared and unbuildable — the data offers what no factory makes:\n  {string.Join("\n  ", unbuildable)}");
            Assert.IsTrue(catalog.All.Count > 0, "the shipped data declares no augment, so the walk proves nothing");
        }

        [TestMethod]
        public void EveryFactoryHasARecordThatDeclaresIt()
        {
            (AbilityProvider registryHolder, AbilityAugmentCatalog catalog) = ShippedAbilityData.Load();
            var declared = DeclaredIds(catalog).ToHashSet(StringComparer.Ordinal);

            var unreachable = registryHolder.BuildableAugmentIds.Where(id => !declared.Contains(id)).OrderBy(id => id).ToList();

            Assert.AreEqual(0, unreachable.Count,
                $"registered and undeclared — the code builds what no record names:\n  {string.Join("\n  ", unreachable)}");
            Assert.IsTrue(registryHolder.BuildableAugmentIds.Count > 0, "the registry is empty, so the walk proves nothing");
        }

        [TestMethod]
        public void TheSectionDeclaresEachAugmentOnce()
        {
            // The index is keyed by id, so a repeated id keeps the last record and loses the rest
            // without a word. It would also leave the two sets above matching, which is why the
            // count is held against the file rather than against the index built from it.
            List<string> written = DeclaredAugmentIds();
            var duplicates = written.GroupBy(id => id, StringComparer.Ordinal)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .OrderBy(id => id)
                .ToList();

            Assert.AreEqual(0, duplicates.Count, $"declared more than once: {string.Join(", ", duplicates)}");
            Assert.AreEqual(written.Count, ShippedAbilityData.Augments().All.Count,
                "the catalog holds fewer records than the section writes");
        }

        /// <summary>Every augment id the catalog holds.</summary>
        private static IEnumerable<string> DeclaredIds(AbilityAugmentCatalog catalog) =>
            catalog.All.Select(record => record.Id);

        /// <summary>Every augment id the shipped section writes, read off the files themselves —
        /// repetitions included, which is the point.</summary>
        private static List<string> DeclaredAugmentIds()
        {
            List<string> ids = [];

            foreach (string path in Directory.EnumerateFiles(SharedData.Catalog(DataCatalog.Abilities), "*.json", SearchOption.AllDirectories))
                foreach (JObject augment in Augments(JObject.Parse(File.ReadAllText(path))))
                    ids.Add(augment.Value<string>("id") ?? string.Empty);

            return ids;
        }

        private static IEnumerable<JObject> Augments(JObject root) =>
            root["augments"] is { } section ? section.OfType<JObject>() : [];
    }
}

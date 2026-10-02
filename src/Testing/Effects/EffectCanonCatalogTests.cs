namespace LastBreathTest.Effects
{
    using Battle.Source;
    using Core.Battle.Abilities;
    using Core.Data.GameData;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// The catalog that reads <c>SharedData/Effects</c> and the registry that asks it. The two used to be
    /// one class, so the rows and the factories were loaded together and could not come apart; now the
    /// canon is read by a participant every composition holds and the registry asks it at the moment of
    /// the question — which is the thing walked here, because a registry that copied the rows when it was
    /// built would answer from an empty canon for the rest of the run and nothing would say so.
    /// </summary>
    [TestClass]
    public class EffectCanonCatalogTests
    {
        /// <summary>A shipped row with a ceiling on it, used wherever a walk needs a real id.</summary>
        private const string ProbeId = "Effect_Clumsiness";

        /// <summary>An id no file declares.</summary>
        private const string UnknownId = "Effect_No_Such_Thing";

        private const string MaxStacksKey = "maxStacks";

        [TestMethod]
        public void TheCatalogAnswersForEveryShippedRowAsTheFileWritesIt()
        {
            var catalog = new EffectCanonCatalog();
            EffectProviders.ShippedRowsInto(catalog);
            List<(string Id, Dictionary<string, float> Figures, string? Power)> rows = [.. ShippedRows()];

            Assert.IsTrue(rows.Count > 0, "the canonical effect catalog was not read at all, so this walk proves nothing");
            CollectionAssert.AreEquivalent(rows.Select(row => row.Id).ToList(), catalog.Ids.ToList(),
                "the catalog carries other ids than the shipped files declare");

            List<string> divergent = [];
            foreach ((string id, Dictionary<string, float> figures, string? power) in rows)
            {
                IReadOnlyDictionary<string, float>? canonical = catalog.CanonOf(id);
                if (canonical == null || figures.Except(canonical).Any() || canonical.Except(figures).Any())
                    divergent.Add($"{id}: the file says [{Written(figures)}], the catalog says [{Written(canonical)}]");

                int? ceiling = figures.TryGetValue(MaxStacksKey, out float stacks) ? (int)stacks : null;
                if (ceiling != catalog.StackCeilingOf(id))
                    divergent.Add($"{id}: the file balances {ceiling?.ToString() ?? "no"} stacks, the catalog says {catalog.StackCeilingOf(id)}");

                EffectPower named = power == null ? EffectPower.Weak : Enum.Parse<EffectPower>(power, ignoreCase: true);
                if (named != catalog.PowerOf(id)) divergent.Add($"{id}: the row says {named}, the catalog says {catalog.PowerOf(id)}");
            }

            Assert.AreEqual(0, divergent.Count, $"the catalog against the rows as written:\n  {string.Join("\n  ", divergent)}");
        }

        [TestMethod]
        public void AnIdTheCanonCarriesNoRowForIsAnsweredTheWayItAlwaysWas()
        {
            var catalog = new EffectCanonCatalog();
            EffectProviders.ShippedRowsInto(catalog);

            Assert.IsNull(catalog.CanonOf(UnknownId), "the catalog invented figures for an id no file declares");
            Assert.IsNull(catalog.StackCeilingOf(UnknownId), "the catalog invented a ceiling for an id no file declares");
            Assert.AreEqual(EffectPower.Weak, catalog.PowerOf(UnknownId), "an id no row names came out stronger than weak");
        }

        [TestMethod]
        public void TheRegistryTakesItsCanonFromTheCatalogAtTheMomentItIsAsked()
        {
            // The order the game composes in: the registry is built first and the files are read after,
            // by a participant of a load it takes no part in. A registry holding a canon of its own —
            // copied at construction or filled by a load it sat through — answers nothing here.
            var catalog = new EffectCanonCatalog();
            var registry = new EffectProvider(catalog);

            Assert.IsNull(registry.CanonOf(ProbeId), "the registry answered for a canon nothing had read yet");

            EffectProviders.ShippedRowsInto(catalog);

            (string _, Dictionary<string, float> figures, string? power) = ShippedRows().Single(row => row.Id == ProbeId);
            CollectionAssert.AreEquivalent(figures.ToList(), registry.CanonOf(ProbeId)?.ToList(),
                "the registry kept a canon of its own instead of asking the catalog");
            Assert.AreEqual((int)figures[MaxStacksKey], registry.StackCeilingOf(ProbeId), "the ceiling came from somewhere other than the catalog");
            Assert.AreEqual(power == null ? EffectPower.Weak : Enum.Parse<EffectPower>(power, ignoreCase: true), registry.PowerOf(ProbeId),
                "the strength came from somewhere other than the catalog");
        }

        [TestMethod]
        public void ARowNamingAnEffectNothingBuildsIsRefusedByTheRegistryAndNotByTheCatalog()
        {
            // The two halves of the split. The catalog takes the row as written — it knows nothing about
            // what builds an effect — and the registry, which does, refuses to answer for it. The refusal
            // used to happen as the file was read, and moving it to first use must not have lost it.
            const string Unbuildable = "Effect_Nothing_Builds_This";
            var catalog = new EffectCanonCatalog();
            catalog.Apply(DataCatalog.Effects, new GameDataFile("probe.json", Row(Unbuildable, new { duration = 3 })));

            Assert.IsNotNull(catalog.CanonOf(Unbuildable), "the catalog dropped a row it has no opinion about");
            Assert.IsNull(new EffectProvider(catalog).CanonOf(Unbuildable),
                "the registry answered from a row it cannot build anything out of");
        }

        [TestMethod]
        public void ARowTheFactoryCannotTakeWholeIsWithheldRatherThanHalfUsed()
        {
            // A key nothing reads is the typo trap the declared keys exist for, and a row carrying one is
            // refused entire — a half-taken row would tune an effect at figures nobody balanced.
            EffectProvider registry = EffectProviders.FromJson(
                Row(ProbeId, new { duration = 3, maxStacks = 5, valeu = 0.15f }));

            Assert.IsNull(registry.CanonOf(ProbeId), "a row the factory cannot take whole was answered for anyway");
            Assert.IsNull(registry.StackCeilingOf(ProbeId), "a refused row still handed out its ceiling");
        }

        /// <summary>A canonical file carrying one row, written the way the shipped ones are.</summary>
        private static string Row(string id, object properties) =>
            JsonConvert.SerializeObject(new { effects = new[] { new { id, properties } } });

        /// <summary>Every shipped canonical row as written: id, figures and the strength it names.</summary>
        private static IEnumerable<(string Id, Dictionary<string, float> Figures, string? Power)> ShippedRows()
        {
            foreach (string file in Directory.EnumerateFiles(
                         SharedData.Catalog(DataCatalog.Effects), "*.json", SearchOption.AllDirectories))
                foreach (JObject entry in (JObject.Parse(File.ReadAllText(file))["effects"] as JArray ?? []).OfType<JObject>())
                {
                    Dictionary<string, float> figures = entry["properties"] is JObject properties
                        ? properties.Properties().ToDictionary(property => property.Name, property => (float)property.Value!, StringComparer.Ordinal)
                        : new Dictionary<string, float>(StringComparer.Ordinal);
                    yield return ((string?)entry["id"] ?? string.Empty, figures, (string?)entry["power"]);
                }
        }

        private static string Written(IReadOnlyDictionary<string, float>? figures) =>
            figures == null ? "nothing" : string.Join(", ", figures.OrderBy(figure => figure.Key, StringComparer.Ordinal).Select(figure => $"{figure.Key}={figure.Value}"));
    }
}

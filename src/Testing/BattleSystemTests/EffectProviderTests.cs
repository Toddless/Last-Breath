namespace LastBreathTest.BattleSystemTests
{
    using Battle.Source;
    using Core.Battle.Abilities;
    using Core.Battle.Skills;
    using Core.Data.GameData;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// The one registry that turns an id and a handful of numbers into an effect. Two things about it
    /// fail in silence and are walked here: a record naming an effect nothing builds, and a property key
    /// nothing reads — the second is the trap the item grants already fell into once, where a renamed
    /// key left the grant minted, worn and doing nothing.
    /// </summary>
    [TestClass]
    public class EffectProviderTests
    {
        [TestMethod]
        public void EveryFactoryBuildsItsEffectFromExactlyTheKeysItDeclares()
        {
            // Declared keys are a promise in two directions: the factory reads no key it did not name
            // (or the walk below would hand it everything and still fail) and needs no key beyond them.
            var provider = new EffectProvider();

            Assert.IsTrue(provider.KnownIds.Count > 0, "the registry is empty, so the walks prove nothing");

            foreach (string id in provider.KnownIds)
            {
                IReadOnlyCollection<string>? keys = provider.KeysOf(id);
                Assert.IsNotNull(keys, $"'{id}' is offered and has no declared keys");

                IEffect? built = provider.CreateEffect(id, PropertiesFor(id, keys));

                Assert.IsNotNull(built, $"'{id}' refused the very keys it declares");
                Assert.AreEqual(id, built.Id, $"'{id}' built an effect that calls itself '{built.Id}'");
            }
        }

        [TestMethod]
        public void AKeyNobodyReadsIsRefusedRatherThanIgnored()
        {
            // The whole point of declaring keys. A misspelled property used to ride along unread: the
            // effect was built, tuned by whatever was left, and nothing said so.
            var provider = new EffectProvider();
            const string Id = "Effect_Clumsiness";
            IReadOnlyCollection<string> keys = provider.KeysOf(Id)!;

            var withTypo = new Dictionary<string, float>(StringComparer.Ordinal);
            foreach (string key in keys) withTypo[key] = 1f;
            withTypo["valeu"] = 1f;

            Assert.IsNull(provider.CreateEffect(Id, new SkillProperties(Id, withTypo)),
                "a property nothing reads was accepted, so a typo in data stays invisible");
        }

        [TestMethod]
        public void AMissingKeyIsRefusedRatherThanGuessed()
        {
            var provider = new EffectProvider();
            const string Id = "Effect_Clumsiness";
            IReadOnlyCollection<string> keys = provider.KeysOf(Id)!;

            var short_ = new Dictionary<string, float>(StringComparer.Ordinal);
            foreach (string key in keys.Skip(1)) short_[key] = 1f;

            Assert.IsNull(provider.CreateEffect(Id, new SkillProperties(Id, short_)),
                "an effect was built without one of its numbers");
        }

        [TestMethod]
        public void AnIdNobodyBuildsIsRefused()
        {
            Assert.IsNull(new EffectProvider().CreateEffect("Effect_No_Such_Thing", SkillProperties.Empty),
                "an id nothing answers came back with an effect");
        }

        [TestMethod]
        public void EveryEffectTheShippedDataNamesIsBuildableFromTheKeysItCarries()
        {
            // The half that will carry the weight once records name effects themselves (wave C-2). Today
            // the shipped catalog grants passives only, so the walk asserts it READ the catalog rather
            // than passing on an empty list — a vacuous green here would be the same silence again.
            var provider = new EffectProvider();
            List<(string Id, List<string> Keys)> used = [.. EffectGrantsInData()];
            List<string> broken = [];

            foreach ((string id, List<string> keys) in used)
            {
                IReadOnlyCollection<string>? declared = provider.KeysOf(id);
                if (declared == null)
                {
                    broken.Add($"{id}: nothing builds it");
                    continue;
                }

                List<string> unknown = [.. keys.Except(declared, StringComparer.Ordinal)];
                List<string> missing = [.. declared.Except(keys, StringComparer.Ordinal)];
                if (unknown.Count > 0 || missing.Count > 0)
                    broken.Add($"{id}: unknown [{string.Join(", ", unknown)}], missing [{string.Join(", ", missing)}]");
            }

            Assert.AreEqual(0, broken.Count, $"records naming effects the registry cannot build:\n  {string.Join("\n  ", broken)}");
            // Not just "the catalog was read": the KINDS it holds are written out. The walk selects
            // effect-kind grants, and a renamed kind would leave that selection empty for good while
            // this stayed green — the silence it exists to break.
            var kinds = KindsInData();
            Assert.IsTrue(kinds.Count > 0, "the shipped effect catalog was not read at all, so this walk proves nothing");
            CollectionAssert.AreEquivalent(new[] { "Passive" }, kinds.ToList(),
                "the kinds the effect catalog declares changed — check that the effect-grant selection above still selects anything");
        }

        /// <summary>Every effect-kind grant the shipped catalog declares, with the keys it carries.</summary>
        private static IEnumerable<(string Id, List<string> Keys)> EffectGrantsInData()
        {
            foreach (JObject entry in CatalogEntries())
            {
                if (!string.Equals((string?)entry["kind"], "Effect", StringComparison.OrdinalIgnoreCase)) continue;

                List<string> keys = entry["properties"] is JObject properties
                    ? [.. properties.Properties().Select(property => property.Name)]
                    : [];
                yield return ((string?)entry["id"] ?? string.Empty, keys);
            }
        }

        /// <summary>Every distinct grant kind the shipped catalog declares.</summary>
        private static HashSet<string> KindsInData() =>
            [.. CatalogEntries().Select(entry => (string?)entry["kind"] ?? string.Empty)];

        private static IEnumerable<JObject> CatalogEntries()
        {
            string path = SharedData.Catalog(DataCatalog.ItemEffects);
            if (!Directory.Exists(path)) yield break;

            foreach (string file in Directory.EnumerateFiles(path, "*.json", SearchOption.AllDirectories))
                foreach (JObject entry in (JObject.Parse(File.ReadAllText(file))["effects"] as JArray ?? []).OfType<JObject>())
                    yield return entry;
        }

        private static SkillProperties PropertiesFor(string id, IEnumerable<string> keys) =>
            new(id, keys.ToDictionary(key => key, _ => 1f, StringComparer.Ordinal));
    }
}

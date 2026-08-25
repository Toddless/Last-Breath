namespace LastBreathTest.BattleSystemTests
{
    using Battle.Source;
    using Core.Battle;
    using Core.Battle.Skills;
    using Core.Data.GameData;

    /// <summary>
    /// The shipped passive catalog is the authoring tool's copy of a registry that lives battle-side,
    /// where the tool cannot reach it. Nothing joins the two at run time — the tool would suggest an id
    /// no factory answers to, and the node authored from it would hand the player nothing. This is that
    /// join, made at build time: same ids both ways, and the same fields each of them is tuned by.
    /// </summary>
    [TestClass]
    public class PassiveCatalogSyncTests
    {
        /// <summary>Stands in for balance the factories never look at — every field is read as a number
        /// and none of them is validated at construction.</summary>
        private const float AnyValue = 1f;

        private const string CatalogFile = "PassiveCatalog.json";

        [TestMethod]
        public void EveryCatalogIdIsBuiltByTheRegistry()
        {
            PassiveSkillCatalog catalog = Shipped();
            var provider = new PassiveSkillProvider();

            List<string> refused = [];
            foreach (string id in catalog.Ids)
                if (provider.CreateSkill(id, Fields(id, catalog.RequiredFields(id))) is null)
                    refused.Add(id);

            Assert.AreEqual(0, refused.Count,
                $"catalog ids no factory builds from the fields the catalog declares: {string.Join(", ", refused)}");
        }

        [TestMethod]
        public void TheCatalogAndTheRegistryNameTheSamePassives()
        {
            var catalogIds = new HashSet<string>(Shipped().Ids, StringComparer.Ordinal);
            var registryIds = new HashSet<string>(PassiveSkillProvider.NamedIds, StringComparer.Ordinal);

            Assert.AreEqual(0, catalogIds.Except(registryIds).Count(),
                $"catalog ids no factory answers to: {string.Join(", ", catalogIds.Except(registryIds).Order())}");

            Assert.AreEqual(0, registryIds.Except(catalogIds).Count(),
                $"factories the catalog never offers the author: {string.Join(", ", registryIds.Except(catalogIds).Order())}");
        }

        /// <summary>A field the catalog demands but the factory never reads makes the tool ask the author
        /// for balance nobody spends. Shown by taking each field away: the grant has to fail without it.</summary>
        [TestMethod]
        public void EveryDeclaredFieldIsOneTheFactoryReads()
        {
            PassiveSkillCatalog catalog = Shipped();
            var provider = new PassiveSkillProvider();

            List<string> unread = [];
            foreach (string id in catalog.Ids)
            {
                IReadOnlyList<string> fields = catalog.RequiredFields(id);
                for (int index = 0; index < fields.Count; index++)
                {
                    List<string> without = [.. fields];
                    without.RemoveAt(index);

                    if (provider.CreateSkill(id, Fields(id, without)) is not null) unread.Add($"{id}.{fields[index]}");
                }
            }

            Assert.AreEqual(0, unread.Count,
                $"fields the catalog demands that no factory reads: {string.Join(", ", unread)}");
        }

        /// <summary>The stat family is any id under its prefix, built from whatever fields the author
        /// writes — listing one would freeze a family whose whole point is that nobody has to.</summary>
        [TestMethod]
        public void TheStatFamilyIsAnsweredByItsPrefixRatherThanListed()
        {
            List<string> listed = [.. Shipped().Ids.Where(StatPassiveGrammar.Owns)];

            Assert.AreEqual(0, listed.Count,
                $"stat-family ids written into the catalog: {string.Join(", ", listed)}");
        }

        [TestMethod]
        public void TheShippedCatalogIsNotEmpty() =>
            Assert.IsTrue(Shipped().Ids.Count > 0, "an empty catalog would make every guard here check nothing");

        private static PassiveSkillCatalog Shipped()
        {
            string path = Path.Combine(SharedData.Catalog(DataCatalog.PassiveSkills), CatalogFile);
            Assert.IsTrue(File.Exists(path), $"the passive catalog ships at {path}");

            return PassiveSkillCatalog.Read(File.ReadAllText(path));
        }

        private static RecordProperties Fields(string id, IEnumerable<string> names)
        {
            var values = new Dictionary<string, float>(StringComparer.Ordinal);
            foreach (string name in names) values[name] = AnyValue;

            return new RecordProperties(id, values);
        }
    }
}

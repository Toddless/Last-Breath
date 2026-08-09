namespace LastBreathTest.CraftingSystemTests
{
    using Core.Data;
    using Core.Data.GameData;

    /// <summary>
    /// The EquipItemResources catalog ships as a BARE ARRAY of item/resource lines. A parser reading an
    /// object wrapper instead threw <c>JsonSerializationException</c> on every boot and the whole catalog
    /// dropped out — visible only as one Tracker line ("Failed to load game data") while the game carried
    /// on with an empty table (runtime log, 2026-07-08 15:23/15:25/15:26; parser corrected 2026-07-10).
    /// What is pinned here is the join the game makes at boot: the shipped file, read by the parser that
    /// reads it, with the shape of the result checked rather than merely "did not throw".
    /// </summary>
    [TestClass]
    public class EquipItemResourcesSchemaTests
    {
        private static readonly DataParser s_parser = new(new LootGeneration.Internal.ItemGameDataFactory());

        [TestMethod]
        public void TheShippedFileIsReadByTheParserThatReadsItAtBoot()
        {
            string path = Path.Combine(SharedData.Catalog(DataCatalog.EquipItemResources), "EquipItemResources.json");
            Assert.IsTrue(File.Exists(path), $"the shipped equip item resources are missing at {path}");

            var resources = s_parser.ParseEquipItemResources(File.ReadAllText(path));

            // An empty table is exactly what a swallowed parse failure leaves behind, so the count is
            // the assertion — a file that parses into nothing is the bug wearing a green test.
            Assert.IsTrue(resources.Count > 0, "the shipped file parsed into an empty table");
            foreach ((string itemId, var lines) in resources)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(itemId), "an entry ships without an itemId");
                Assert.IsTrue(lines.Count > 0, $"{itemId}: entry ships with no resource lines");
            }
        }
    }
}

namespace LastBreathTest.World
{
    using System.Collections.Generic;
    using Core.World.DualGrid;

    /// <summary>
    /// The naming convention of the terrain container: a data layer is served by the config entry of the same
    /// name, and every name that does not pair up is named out loud. These are the cases that would otherwise
    /// show up in the world as a terrain that simply is not there.
    /// </summary>
    [TestClass]
    public class TerrainRosterTests
    {
        private static readonly string[] s_forest = ["Dirt", "Turf", "Water", "TallGrass"];

        [TestMethod]
        public void MatchingNamesLeaveNothingToReport()
        {
            TerrainRosterReport report = TerrainRoster.Match(s_forest, s_forest);

            Assert.IsTrue(report.IsClean, "a roster that pairs up one to one still has a complaint");
        }

        [TestMethod]
        public void OrderOfTheNamesDoesNotMatter()
        {
            TerrainRosterReport report = TerrainRoster.Match(s_forest, ["Water", "TallGrass", "Dirt", "Turf"]);

            Assert.IsTrue(report.IsClean, "the roster read the entry order as a mismatch");
        }

        [TestMethod]
        public void ALayerWithoutAnEntryIsNamed()
        {
            TerrainRosterReport report = TerrainRoster.Match(s_forest, ["Dirt", "Turf", "Water"]);

            CollectionAssert.AreEqual(new List<string> { "TallGrass" }, new List<string>(report.LayersWithoutEntry));
            Assert.AreEqual(0, report.EntriesWithoutLayer.Count, "an unpaired layer was also read as an unpaired entry");
            Assert.IsFalse(report.IsClean);
        }

        [TestMethod]
        public void AnEntryWithoutALayerIsNamed()
        {
            TerrainRosterReport report = TerrainRoster.Match(["Dirt", "Turf"], ["Dirt", "Turf", "Water"]);

            CollectionAssert.AreEqual(new List<string> { "Water" }, new List<string>(report.EntriesWithoutLayer));
            Assert.AreEqual(0, report.LayersWithoutEntry.Count, "an unpaired entry was also read as an unpaired layer");
        }

        [TestMethod]
        public void BothSidesOfATypoAreNamed()
        {
            TerrainRosterReport report = TerrainRoster.Match(["Dirt", "Tallgrass"], ["Dirt", "TallGrass"]);

            CollectionAssert.AreEqual(new List<string> { "Tallgrass" }, new List<string>(report.LayersWithoutEntry));
            CollectionAssert.AreEqual(new List<string> { "TallGrass" }, new List<string>(report.EntriesWithoutLayer));
        }

        [TestMethod]
        public void ARepeatedLayerNameIsReportedOnceAndStillCountsAsPaired()
        {
            TerrainRosterReport report = TerrainRoster.Match(["Dirt", "Dirt", "Dirt", "Water"], ["Dirt", "Water"]);

            CollectionAssert.AreEqual(new List<string> { "Dirt" }, new List<string>(report.DuplicateLayers));
            Assert.AreEqual(0, report.LayersWithoutEntry.Count, "the repeat was counted as a second, unserved layer");
            Assert.AreEqual(0, report.EntriesWithoutLayer.Count);
        }

        [TestMethod]
        public void ARepeatedEntryKeyIsReportedOnce()
        {
            TerrainRosterReport report = TerrainRoster.Match(["Dirt"], ["Dirt", "Dirt"]);

            CollectionAssert.AreEqual(new List<string> { "Dirt" }, new List<string>(report.DuplicateEntries));
            Assert.AreEqual(0, report.EntriesWithoutLayer.Count, "the repeat was counted as an entry of its own");
        }

        [TestMethod]
        public void BlankEntryKeysAreCountedInsteadOfMatched()
        {
            TerrainRosterReport report = TerrainRoster.Match(["Dirt"], ["Dirt", "", "   "]);

            Assert.AreEqual(2, report.UnnamedEntries, "blank keys were not counted");
            Assert.AreEqual(0, report.EntriesWithoutLayer.Count, "a blank key was reported as a named entry");
            Assert.AreEqual(0, report.DuplicateEntries.Count, "two blanks were reported as a repeated key");
            Assert.IsFalse(report.IsClean, "a config with empty rows passed as clean");
        }

        [TestMethod]
        public void AnEmptyContainerWithAnEmptyConfigIsClean()
        {
            TerrainRosterReport report = TerrainRoster.Match([], []);

            Assert.IsTrue(report.IsClean, "an empty container has something to complain about");
        }

        [TestMethod]
        public void UnpairedNamesKeepTheOrderTheyWereMetIn()
        {
            TerrainRosterReport report = TerrainRoster.Match(["Ash", "Dirt", "Snow"], ["Dirt", "Lava", "Mud"]);

            CollectionAssert.AreEqual(new List<string> { "Ash", "Snow" }, new List<string>(report.LayersWithoutEntry));
            CollectionAssert.AreEqual(new List<string> { "Lava", "Mud" }, new List<string>(report.EntriesWithoutLayer));
        }
    }
}

namespace LastBreathTest
{
    using System.Reflection;
    using Core.Data.GameData;

    /// <summary>
    /// A participant names the catalogs it reads; the folder behind that name ships separately. Nothing
    /// joins the two until the game boots, so a name can be added, registered and merged with no data
    /// behind it — the load reports the miss to the log and the participant quietly keeps its code
    /// defaults, which is exactly the shape a balance file rots in. This is that join, made at build time.
    /// </summary>
    [TestClass]
    public class ShippedCatalogTests
    {
        /// <summary>What <see cref="DataCatalog"/> promises, checked where the game reads it from. An
        /// empty folder counts as missing: the source hands the participant nothing and says nothing,
        /// which is the same silence as no folder at all with none of the warning.</summary>
        [TestMethod]
        public void EveryCatalogNameHasShippedDataBehindIt()
        {
            IReadOnlyList<string> catalogs = CatalogNames();
            Assert.IsTrue(catalogs.Count > 0, "no catalog names were found — the guard is checking nothing");

            List<string> unbacked = [];
            foreach (string catalog in catalogs)
            {
                string folder = SharedData.Catalog(catalog);
                if (!Directory.Exists(folder))
                    unbacked.Add($"{catalog} (no folder at {folder})");
                else if (Directory.GetFiles(folder, "*.json", SearchOption.AllDirectories).Length == 0)
                    unbacked.Add($"{catalog} (folder ships no .json)");
            }

            Assert.AreEqual(0, unbacked.Count,
                $"catalogs named in code with no shipped data behind them: {string.Join(", ", unbacked)}");
        }

        private static IReadOnlyList<string> CatalogNames() =>
            typeof(DataCatalog)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => field is { IsLiteral: true, IsInitOnly: false } && field.FieldType == typeof(string))
                .Select(field => (string)field.GetRawConstantValue()!)
                .ToList();
    }
}

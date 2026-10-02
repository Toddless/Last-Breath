namespace LastBreathTest.Items
{
    using Core.Data.GameData;
    using LastBreathTest.Descriptors;
    using Tooling.Catalogs.Checks;

    /// <summary>
    /// A range field of the equipment catalog is written as an object with two bounds, and only so.
    /// <para>The converters also read a bare number as a fixed range, which is why nothing but a guard keeps
    /// the two forms from spreading side by side: a scalar loads, plays and reviews the same, so the file
    /// grows a second spelling of one field that every reader — the game, the authoring tool, the next
    /// audit — has to know about. The tolerance stays for old files; the shipped ones are one shape.</para>
    /// <para>WHICH fields are ranges is read off the schema, through the same checks the authoring tool
    /// presses its button on: a pair of bounds is a record of two numbers named min and max, so a DTO that
    /// grows one joins the rule without anyone remembering it. A gate here and a report there.</para>
    /// </summary>
    [TestClass]
    public class EquipItemDataFormAuditTests
    {
        /// <summary>A template whose roll is written as a plain number beside one written as a pair of
        /// bounds: the walk has to say the first and stay quiet about the second.</summary>
        private const string ForgedEquipItemJson = """
        {
          "items": [
            {
              "id": "Weapon_Forged_Scalar",
              "slot": "Weapon",
              "rarity": "Rare",
              "modifiers": [ { "parameter": "PhysicalDamage", "modifierType": "inc", "value": 1 } ]
            },
            {
              "id": "Weapon_Forged_Bounded",
              "slot": "Weapon",
              "rarity": "Rare",
              "modifiers": [ { "parameter": "PhysicalDamage", "modifierType": "inc", "value": { "min": 1, "max": 2 } } ]
            }
          ]
        }
        """;

        /// <summary>Both halves of the rule at once: a range written as a plain number, and one written as
        /// an object whose bound the converter cannot read. They are one finding because they are one
        /// mistake — the record is dropped at load, or rolls around nothing.</summary>
        [TestMethod]
        public void NoRangeOfTheShippedEquipmentIsWrittenAsAnythingButAPairOfBounds()
        {
            string[] malformed =
            [
                .. CatalogCrossCheckTests.Shipped.Findings
                    .Where(finding => finding.Kind == CatalogFindingKind.MalformedRange
                                      && finding.Catalog == DataCatalog.EquipItems)
                    .Select(finding => $"{finding.Record}  {finding.Where}")
            ];

            Assert.AreEqual(0, malformed.Length,
                "A range of the equipment catalog is written as an object with both bounds, so that one field has one "
                + $"shape wherever it stands. Written otherwise:{Environment.NewLine}  {string.Join($"{Environment.NewLine}  ", malformed)}");
        }

        /// <summary>The mutation the gate exists for, and the proof it reaches a range at all: a shipped
        /// catalog whose ranges the walk never met would pass the gate above for ever.</summary>
        [TestMethod]
        public void ARangeOfTheEquipmentWrittenAsANumber_IsCaught()
        {
            IReadOnlyList<CatalogFinding> found = CatalogCrossCheckTests.Forged(DataCatalog.EquipItems, ForgedEquipItemJson);

            CollectionAssert.AreEqual(
                new[] { "value" },
                CatalogCrossCheckTests.Words(found, CatalogFindingKind.MalformedRange),
                $"the forged templates were read as: {CatalogCrossCheckTests.Lines(found)}");
        }
    }
}

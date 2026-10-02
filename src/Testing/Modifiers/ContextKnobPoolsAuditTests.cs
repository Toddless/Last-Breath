namespace LastBreathTest.Modifiers
{
    using Core.Crafting;
    using Core.Data;
    using Core.Data.GameData;
    using Core.Data.Validation;
    using Core.Enums;
    using Core.Modifiers;
    using LastBreathTest.Descriptors;
    using LootGeneration.Internal;
    using Tooling.Catalogs.Checks;

    /// <summary>
    /// A context knob's binding is picked by the parameter and reads the line's number; the value type the
    /// line was written as reaches no pipeline at all — healing efficiency works the same written flat or
    /// written increased. Duplicate control disagrees: a line's identity counts the value type, so one knob
    /// written in two of them is two identities. An item may then roll both entries, each attaches on its
    /// own, the handler applies them one after the other and they multiply — a bonus larger than either line
    /// says, that no duplicate check can see.
    /// <para>The rule keeping that unreachable is the game's own (<see cref="ContextKnobRules"/>), which the
    /// authoring tool reads over the documents an author has open. It stays a GATE here — the tool reports
    /// and the game refuses.</para>
    /// </summary>
    [TestClass]
    public class ContextKnobPoolsAuditTests
    {
        /// <summary>The pool a mutation is written into: one knob written both ways, the second of them
        /// buried in a bundle, because a part of a composite attaches like any other line.</summary>
        private const string SplitKnobPoolJson = """
        {
            "pools": [
                {
                    "id": "Forged_Split_Knob",
                    "modifiersPool": [
                        {
                            "parameter": "HealingEfficiency",
                            "modifierType": "flat",
                            "value": { "min": 0.1, "max": 0.2 },
                            "weight": 50,
                            "affix": "Prefix"
                        },
                        {
                            "weight": 50,
                            "affix": "Suffix",
                            "parts": [
                                {
                                    "parameter": "HealingEfficiency",
                                    "modifierType": "increase",
                                    "value": { "min": 0.1, "max": 0.2 }
                                }
                            ]
                        }
                    ]
                }
            ]
        }
        """;

        [TestMethod]
        public void EveryContextKnob_IsWrittenInOneValueTypeAcrossTheRollablePools()
        {
            IReadOnlyList<RollablePool> pools = ShippedPools();

            // The knobs and not the lines: the pools are full of plain parameters, and a run that met no
            // knob at all would agree with itself forever.
            Assert.AreNotEqual(0, ContextKnobRules.Knobs(pools), "No shipped pool holds a context knob — the audit has nothing to audit.");

            IReadOnlyList<DataFinding> split = ContextKnobRules.Check(pools);

            Assert.AreEqual(0, split.Count,
                "A context knob is applied by its parameter alone — entries of one knob written in different value types "
                + "are the same bonus twice, and an item that rolls both wears them multiplied. Write each knob's entries "
                + $"in one value type:\n  {string.Join("\n  ", split.Select(finding => finding.Message))}");
        }

        /// <summary>The mutation the gate exists for, put to the rule itself: one knob written flat in one
        /// pool and increased inside a bundle in another is one finding naming that knob.</summary>
        [TestMethod]
        public void AKnobWrittenBothFlatAndIncreased_IsCaught()
        {
            IReadOnlyList<DataFinding> split = ContextKnobRules.Check(
            [
                new RollablePool("Forged_Flat", [Knob(ModifierValueType.Flat)]),
                new RollablePool("Forged_Increase", [new CompositeDescriptor([Knob(ModifierValueType.Increase)])]),
            ]);

            Assert.AreEqual(1, split.Count, $"the forged split was read as: {Lines(split)}");
            Assert.AreEqual(DataFindingKind.SplitValueType, split[0].Kind);
            Assert.AreEqual(nameof(ContextParameter.HealingEfficiency), split[0].Named);
        }

        /// <summary>A pool holding no knob at all, and a run holding no pool: neither is a finding, and a
        /// rule that reported one would be a gate every empty catalog fails.</summary>
        [TestMethod]
        public void PoolsWithNoContextKnobs_AreNothingToReport()
        {
            Assert.AreEqual(0, ContextKnobRules.Check([]).Count, "an empty run found something to say");

            IReadOnlyList<DataFinding> plain = ContextKnobRules.Check(
                [new RollablePool("Forged_Plain", [new ParameterDescriptor(EntityParameter.Health, ModifierValueType.Flat, new ValueRange(1, 2), ModifierScope.Local)])]);

            Assert.AreEqual(0, plain.Count, $"a pool of plain lines was read as: {Lines(plain)}");
        }

        /// <summary>The same rule over the documents the authoring tool has open: a split written into the
        /// modifier pools reaches the panel's Check as a finding of its own.</summary>
        [TestMethod]
        public void ASplitKnobInTheDocuments_ReachesTheChecksOfTheTool()
        {
            IReadOnlyList<CatalogFinding> added = CatalogCrossCheckTests.Added(DataCatalog.ModifierPools, SplitKnobPoolJson);

            CollectionAssert.AreEqual(
                new[] { nameof(ContextParameter.HealingEfficiency) },
                added.Select(finding => finding.Named).ToArray(),
                $"the forged pool was read by the tool's checks as:\n  {CatalogCrossCheckTests.Lines(added)}");
        }

        private static ContextDescriptor Knob(ModifierValueType valueType) =>
            new(ContextParameter.HealingEfficiency, valueType, new ValueRange(1, 1));

        private static string Lines(IReadOnlyList<DataFinding> found) =>
            string.Join("\n  ", found.Select(finding => $"{finding.Kind}  {finding.Named}  {finding.Message}"));

        /// <summary>The shipped pools an item's lines are rolled from, each labelled by the id an author will
        /// search the data for. Item, family and mythic pools live in one catalog; material and category
        /// descriptors in the other — a reroll unions them, so for a single item they are one pool. Read
        /// through the real parser: the entries an author writes are exactly the entries the game rolls,
        /// value-type aliases resolved and refused entries already dropped.</summary>
        private static IReadOnlyList<RollablePool> ShippedPools()
        {
            var parser = new DataParser(new ItemGameDataFactory());
            List<RollablePool> pools = [];

            foreach (string file in CatalogFiles(DataCatalog.ModifierPools))
                foreach ((string id, var pool) in parser.ParseEquipItemModifierPools(File.ReadAllText(file)))
                    pools.Add(new RollablePool(id, pool));

            foreach (string file in CatalogFiles(DataCatalog.Resources))
            {
                var materials = parser.ParseResources(File.ReadAllText(file))
                    .OfType<ICraftingResource>()
                    .Where(resource => resource.Material != null)
                    .ToList();

                foreach (var category in materials
                    .Select(resource => resource.Material!.MaterialCategory)
                    .OfType<IMaterialCategory>()
                    .DistinctBy(category => category.Id))
                    pools.Add(new RollablePool(category.Id, category.Modifiers));

                foreach (var resource in materials)
                    pools.Add(new RollablePool(resource.Id, resource.Material!.Modifiers));
            }

            return pools;
        }

        private static IEnumerable<string> CatalogFiles(string catalog) =>
            Directory.EnumerateFiles(SharedData.Catalog(catalog), "*.json", SearchOption.AllDirectories);
    }
}

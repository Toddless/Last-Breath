namespace LastBreathTest.BattleSystemTests
{
    using Core.Crafting;
    using Core.Data;
    using Core.Data.GameData;
    using Core.Enums;
    using Core.Modifiers;
    using LootGeneration.Internal;

    /// <summary>
    /// A context knob's binding is picked by the parameter and reads the line's number; the value type the
    /// line was written as reaches no pipeline at all — healing efficiency works the same written flat or
    /// written increased. Duplicate control disagrees: a line's identity counts the value type, so one knob
    /// written in two of them is two identities. An item may then roll both entries, each attaches on its
    /// own, the handler applies them one after the other and they multiply — a bonus larger than either line
    /// says, that no duplicate check can see.
    /// <para>This audit keeps that unreachable from the shipped data: across every pool an item's lines can
    /// come from, a knob is written in ONE value type. Composite parts count as their own entries — the
    /// schema lets a bundle carry a knob among its parts, and such a part attaches like any other line.</para>
    /// </summary>
    [TestClass]
    public class ContextKnobPoolsAuditTests
    {
        [TestMethod]
        public void EveryContextKnob_IsWrittenInOneValueTypeAcrossTheRollablePools()
        {
            var entries = ShippedContextEntries();
            Assert.IsTrue(entries.Count > 0, "No context lines in the shipped pools — the audit has nothing to audit.");

            var split = entries
                .GroupBy(entry => entry.Parameter)
                .Where(knob => knob.Select(entry => entry.ValueType).Distinct().Count() > 1)
                .OrderBy(knob => knob.Key)
                .Select(Describe)
                .ToList();

            Assert.AreEqual(0, split.Count,
                "A context knob is applied by its parameter alone — entries of one knob written in different value types "
                + "are the same bonus twice, and an item that rolls both wears them multiplied. Write each knob's entries "
                + $"in one value type:\n  {string.Join("\n  ", split)}");
        }

        /// <summary>What the data author has to fix: the knob, each value type it is written in, and the
        /// pool, material category or resource id every one of those entries sits under.</summary>
        private static string Describe(IGrouping<ContextParameter, ContextEntry> knob) =>
            $"'{knob.Key}' is written as " + string.Join(" and as ", knob
                .GroupBy(entry => entry.ValueType)
                .OrderBy(bucket => bucket.Key)
                .Select(bucket => $"{bucket.Key} in [{string.Join(", ", bucket.Select(entry => entry.Source).Distinct().Order())}]"));

        private static List<ContextEntry> ShippedContextEntries()
        {
            var found = new List<ContextEntry>();
            var counted = new HashSet<IModifierDescriptor>(ReferenceEqualityComparer.Instance);
            foreach ((string source, var pool) in RollablePools())
                foreach (var entry in pool)
                    Collect(entry, source, counted, found);

            return found;
        }

        /// <summary>Descriptors are gathered once per instance: a material category's entries are the very
        /// objects every material of that category carries, so counting by instance reports them under the
        /// category instead of once per resource that shares them.</summary>
        private static void Collect(IModifierDescriptor descriptor, string source, HashSet<IModifierDescriptor> counted, List<ContextEntry> found)
        {
            if (!counted.Add(descriptor)) return;

            switch (descriptor)
            {
                case CompositeDescriptor composite:
                    foreach (var part in composite.Parts) Collect(part, source, counted, found);
                    break;
                case ContextDescriptor context:
                    found.Add(new ContextEntry(context.Parameter, context.ValueType, source));
                    break;
            }
        }

        /// <summary>The shipped pools an item's lines are rolled from, each labelled by the id an author will
        /// search the data for. Item, family and mythic pools live in one catalog; material and category
        /// descriptors in the other — a reroll unions them, so for a single item they are one pool. Read
        /// through the real parser: the entries an author writes are exactly the entries the game rolls,
        /// value-type aliases resolved and refused entries already dropped.</summary>
        private static IEnumerable<(string Source, IReadOnlyList<IModifierDescriptor> Pool)> RollablePools()
        {
            var parser = new DataParser(new ItemGameDataFactory());

            foreach (string file in CatalogFiles(DataCatalog.ModifierPools))
                foreach ((string id, var pool) in parser.ParseEquipItemModifierPools(File.ReadAllText(file)))
                    yield return (id, pool);

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
                {
                    yield return (category.Id, category.Modifiers);
                }

                foreach (var resource in materials)
                    yield return (resource.Id, resource.Material!.Modifiers);
            }
        }

        private static IEnumerable<string> CatalogFiles(string catalog) =>
            Directory.EnumerateFiles(SharedData.Catalog(catalog), "*.json", SearchOption.AllDirectories);

        private readonly record struct ContextEntry(ContextParameter Parameter, ModifierValueType ValueType, string Source);
    }
}

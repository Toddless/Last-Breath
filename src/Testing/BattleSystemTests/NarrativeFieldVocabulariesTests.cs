namespace LastBreathTest.BattleSystemTests
{
    using System.Collections;
    using Core.Data.GameData;
    using Core.Narrative;
    using LastBreath.Descriptors;
    using Newtonsoft.Json.Linq;
    using Tooling.Catalogs;
    using Tooling.Json;
    using Tooling.Schema;
    using Tooling.Schema.Model;
    using Tooling.Schema.Reflection;

    /// <summary>
    /// The one place saying which keys of the dialogues and the quests are written from the narrative
    /// vocabulary. Nothing else can say it: the DTOs hold those keys as free json, so a key missing from
    /// the table is an editor quietly offering a text box where the author expects a list of conditions,
    /// and a key on the table the DTOs never write is a promise about a key nothing reads.
    /// </summary>
    [TestClass]
    public class NarrativeFieldVocabulariesTests
    {
        /// <summary>The two catalogs whose records are written from the vocabulary.</summary>
        private static readonly string[] s_narrative = [DataCatalog.Dialogues, DataCatalog.Quests];

        /// <summary>The one key of the narrative read as a single entry: every other one is a list, which
        /// is the providers' own reading and not a preference.</summary>
        private const string TheSingleEntryKey = NarrativeFieldVocabularies.Condition;

        private const string ACatalogsOwnField = "npcId";

        /// <summary>Every key the schema of the two catalogs can only call free json is named by the table,
        /// and the table names nothing else. Held in both directions: one way a key goes unedited, the
        /// other a key nobody writes is described.</summary>
        [TestMethod]
        public void TheTableNamesEveryFreeJsonKeyOfTheNarrativeAndNoOthers()
        {
            List<string> written = [.. FreeJsonKeys(s_narrative).Order(StringComparer.Ordinal)];
            List<string> named = [.. NarrativeFieldVocabularies.Fields.Keys.Order(StringComparer.Ordinal)];

            Assert.AreNotEqual(0, written.Count, "the narrative catalogs hold no free json at all — the check is checking nothing");

            List<string> unnamed = [.. written.Except(named, StringComparer.Ordinal)];
            List<string> strangers = [.. named.Except(written, StringComparer.Ordinal)];

            Assert.AreEqual(0, unnamed.Count,
                $"keys of the narrative the table says nothing about: {string.Join(", ", unnamed)}");
            Assert.AreEqual(0, strangers.Count,
                $"keys the table describes and the narrative does not write: {string.Join(", ", strangers)}");
        }

        /// <summary>Every named key is answered with a vocabulary that has words in it, under the key the
        /// parser reads the type by. A binding with no types is a picker with nothing to pick.</summary>
        [TestMethod]
        public void EveryNamedKeyIsAnsweredWithAVocabularyTheParserCanRead()
        {
            Assert.AreNotEqual(0, NarrativeSchemas.Conditions().Count, "the conditions vocabulary is empty");
            Assert.AreNotEqual(0, NarrativeSchemas.Actions().Count, "the actions vocabulary is empty");

            foreach ((string key, VocabularyBinding binding) in NarrativeFieldVocabularies.Fields)
            {
                Assert.AreEqual(NarrativeParameterSchema.TypeKey, binding.TypeKey,
                    $"'{key}' names its type by another key than the parser reads");
                Assert.AreNotEqual(0, binding.Types.Count, $"'{key}' is answered with a vocabulary holding no types");
            }
        }

        /// <summary>An objective's own predicate is one entry; everything else is a list. Reading a list
        /// where the provider parses one entry writes a file it refuses, and the other way round the editor
        /// would offer a second entry the game never reads.</summary>
        [TestMethod]
        public void OnlyTheObjectivesOwnPredicateIsReadAsASingleEntry()
        {
            List<string> single =
                [.. NarrativeFieldVocabularies.Fields.Where(pair => !pair.Value.List).Select(pair => pair.Key)];

            CollectionAssert.AreEqual(new[] { TheSingleEntryKey }, single,
                "another key than the objective's predicate is read as a single entry");
        }

        /// <summary>
        /// The shipped files read against the table: every entry written under a named key names a type
        /// that key's own vocabulary holds. This is what tells the two halves apart — a key answered with
        /// the conditions where the game reads actions would offer the author words the parser drops, and
        /// nothing else in the table could say so.
        /// </summary>
        [TestMethod]
        public void EveryEntryTheShippedNarrativeWritesIsATypeItsKeysVocabularyHolds()
        {
            List<string> strangers = [];
            int counted = 0;

            foreach (string catalog in s_narrative)
                foreach (string file in CatalogWorkspace.FilePaths(SharedData.Catalog(catalog)))
                    Judge(JsonTreeDocument.Load(file).Root, Path.GetFileName(file), strangers, ref counted);

            Assert.AreNotEqual(0, counted, "the shipped narrative writes no entries at all — the check is checking nothing");
            Assert.AreEqual(0, strangers.Count,
                $"entries the vocabulary of the key they stand under does not hold:{Environment.NewLine}  {string.Join($"{Environment.NewLine}  ", strangers)}");
        }

        /// <summary>A condition nested inside another one is answered off the schema and not off the table:
        /// the key it stands under is the composite's own and could be named anything. A resolver reading
        /// names alone would stop the editor at the first <c>AllOf</c>.</summary>
        [TestMethod]
        public void AConditionNestedInsideAnotherIsAnsweredOffTheSchemaWhateverItIsNamed()
        {
            foreach (RecordSchema type in NarrativeSchemas.Conditions())
                foreach (FieldSchema field in type.Fields)
                {
                    if (!Nested(field) && !Nested(field.Item)) continue;

                    VocabularyBinding binding = NarrativeFieldVocabularies.Resolve(field)
                                                ?? throw new AssertFailedException(
                                                    $"'{type.TypeName}.{field.JsonName}' holds conditions and is answered with nothing");

                    Assert.AreEqual(field.Kind == FieldKind.Array, binding.List,
                        $"'{type.TypeName}.{field.JsonName}' is answered with the other shape than it is written in");
                    CollectionAssert.AreEqual(
                        NarrativeSchemas.Conditions().Select(condition => condition.TypeName).ToList(),
                        binding.Types.Select(condition => condition.TypeName).ToList(),
                        $"'{type.TypeName}.{field.JsonName}' is answered with something other than the conditions");
                }
        }

        /// <summary>A field of a catalog's own is answered with nothing: the table is consulted for free
        /// json alone, so a record that one day writes a string called <c>condition</c> is not turned into
        /// a vocabulary the game never reads there.</summary>
        [TestMethod]
        public void AFieldTheSchemaCanVouchForIsAnsweredWithNothing()
        {
            Assert.IsNull(NarrativeFieldVocabularies.Resolve(
                new FieldSchema { JsonName = ACatalogsOwnField, Kind = FieldKind.Reference }));

            Assert.IsNull(NarrativeFieldVocabularies.Resolve(
                new FieldSchema { JsonName = TheSingleEntryKey, Kind = FieldKind.String }));

            Assert.IsNull(NarrativeFieldVocabularies.Resolve(
                new FieldSchema { JsonName = "stages", Kind = FieldKind.Any }));
        }

        /// <summary>
        /// No catalog outside the narrative writes free json under one of the named keys. The resolver is
        /// handed a FIELD and not a catalog — the data editor lists every described catalog and asks the
        /// same table about all of them — so a record elsewhere holding free json called <c>actions</c>
        /// would be drawn as a list of narrative actions the game never reads there.
        /// </summary>
        [TestMethod]
        public void NoCatalogOutsideTheNarrativeWritesFreeJsonUnderANamedKey()
        {
            List<string> elsewhere =
            [
                .. CatalogDescriptors.All
                    .Select(descriptor => descriptor.Catalog)
                    .Where(catalog => !s_narrative.Contains(catalog, StringComparer.Ordinal))
            ];

            Assert.AreNotEqual(0, elsewhere.Count, "the narrative is the only described catalog — the check is checking nothing");

            List<string> claimed =
            [
                .. FreeJsonKeys(elsewhere)
                    .Where(NarrativeFieldVocabularies.Fields.ContainsKey)
                    .Order(StringComparer.Ordinal)
            ];

            Assert.AreEqual(0, claimed.Count,
                $"catalogs outside the narrative write free json under keys the table claims: {string.Join(", ", claimed)}");
        }

        /// <summary>What one key holds, whichever shape it is written in: the elements of a list, or the
        /// lone entry itself.</summary>
        private static IEnumerable<JToken> Entries(JToken value) => value is JArray written ? written : [value];

        /// <summary>Whether a field holds a condition of the vocabulary, which the adapter says by the
        /// record it stands the field on.</summary>
        private static bool Nested(FieldSchema? field) =>
            field?.Record?.TypeName == NarrativeParameterSchema.NestedConditionRecord;

        /// <summary>Every key the given catalogs' schemas call free json, read off the real DTOs.</summary>
        private static HashSet<string> FreeJsonKeys(IEnumerable<string> catalogs)
        {
            HashSet<string> free = new(StringComparer.Ordinal);
            HashSet<RecordSchema> seen = new(ReferenceEqualityComparer.Instance);

            foreach (string catalog in catalogs)
            {
                CatalogSchema schema = new CatalogSchemaBuilder(new SchemaReflector()).Build(Descriptor(catalog));

                foreach (SectionSchema section in schema.Sections) Walk(section.Record, seen, free);
            }

            return free;
        }

        private static ICatalogDescriptor Descriptor(string catalog) =>
            CatalogDescriptors.All.FirstOrDefault(descriptor => descriptor.Catalog == catalog)
            ?? throw new AssertFailedException($"{nameof(CatalogDescriptors)} holds no descriptor of the {catalog} catalog.");

        private static void Walk(RecordSchema record, HashSet<RecordSchema> seen, HashSet<string> free)
        {
            if (!seen.Add(record)) return;

            foreach (FieldSchema field in record.Fields) Walk(field, seen, free);

            if (record.Variants is not { } variants) return;

            foreach (VariantSchema variant in variants.Variants) Walk(variant.Record, seen, free);
        }

        private static void Walk(FieldSchema field, HashSet<RecordSchema> seen, HashSet<string> free)
        {
            if (field.Kind == FieldKind.Any) free.Add(field.JsonName);

            if (field.Record is { } record) Walk(record, seen, free);
            if (field.Item is { } item) Walk(item, seen, free);
            if (field.Key is { } key) Walk(key, seen, free);
        }

        /// <summary>Walks one shipped file, holding every entry written under a named key against the types
        /// that key's vocabulary holds.</summary>
        private static void Judge(JToken token, string file, List<string> strangers, ref int counted)
        {
            if (token is JArray array)
            {
                foreach (JToken element in array) Judge(element, file, strangers, ref counted);
                return;
            }

            if (token is not JObject holder) return;

            foreach (JProperty pair in holder.Properties())
            {
                if (NarrativeFieldVocabularies.Fields.TryGetValue(pair.Name, out VocabularyBinding? binding))
                    foreach (JToken entry in Entries(pair.Value))
                    {
                        counted++;

                        if (TypedRecords.Worn(binding, entry) is null)
                            strangers.Add($"{file}: '{pair.Name}' holds '{TypedRecords.Standing(binding, entry)}'");
                    }

                Judge(pair.Value, file, strangers, ref counted);
            }
        }
    }
}

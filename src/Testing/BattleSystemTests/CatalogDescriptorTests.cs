namespace LastBreathTest.BattleSystemTests
{
    using Core.Ai;
    using Core.Ai.World;
    using Core.Data.GameData;
    using Core.Data.LootTable;
    using Core.Data.NpcData;
    using Core.Data.Schema;
    using Core.Enums;
    using Newtonsoft.Json.Linq;
    using Tooling.Json;
    using Tooling.Schema;
    using Tooling.Schema.Model;
    using Tooling.Schema.Reflection;

    /// <summary>
    /// The join between the game's data and the authoring tool: the descriptors say which catalogs the
    /// tool can be handed a schema for, and the schemas of the described ones are built here from the real
    /// DTOs against the real shipped files.
    /// <para>Nothing else joins the two. A DTO renamed, a reference left unmarked or a catalog added with
    /// no descriptor costs nothing at build time and shows up as an editor quietly offering a text box
    /// where the author expects a picker.</para>
    /// </summary>
    [TestClass]
    public class CatalogDescriptorTests
    {
        /// <summary>Separates the json names of a path walked down a record — <c>reactions.abilityId</c>
        /// is the reaction's ability, whatever lists and maps stand between the two.</summary>
        private const char PathSeparator = '.';

        private const string JsonExtension = ".json";

        /// <summary>What the json name of a field holding an id ends with, whatever its case.</summary>
        private const string IdSuffix = "Id";

        /// <summary>The steps from a loot table down to the positions at it: a table holds tiers, a tier
        /// holds the seats. Neither is named by the descriptor — the DTOs are read for them — so the walk
        /// spells them out.</summary>
        private const string TiersField = "tiers";

        private const string ItemsField = "items";

        /// <summary>What an augment group is written with.</summary>
        private const string TierField = "tier";

        private const string RarityField = "rarity";

        /// <summary>What the reflector may still have to say about the real NPC DTOs. Empty: everything
        /// the walk meets there is a plain property it can read whole. A note appearing here is either a
        /// DTO to fix or a fact to write down — never something to silence by widening the check.</summary>
        private static readonly string[] s_allowedNpcNotes = [];

        /// <summary>
        /// What the reflector has to say about the real loot table DTOs — every one of them a fact about
        /// the shipped types rather than something to be rid of: a position is a positional record and a
        /// group of augments is another, so neither states defaults; the positions of a tier are read
        /// through a converter, which is exactly why the descriptor states their shapes; and whether a
        /// position names anything at all is worked out from the fields around it.
        /// </summary>
        /// <remarks>Held as a set: which notes the DTOs earn is the fact, while the order they are met in
        /// is the walk's business. A note not on this list is a new thing the tool cannot see.</remarks>
        private static readonly string[] s_allowedLootTableNotes =
        [
            $"'{nameof(AugmentGroup)}' cannot be built without arguments, so none of its fields carry a default.",
            $"'{nameof(LootTableTierData)}.{ItemsField}' is read through '{nameof(TableRecordsConverter)}', so the shape of the file may differ from the shape of the type: state it with variants.",
            $"'{nameof(TableRecord)}' cannot be built without arguments, so none of its fields carry a default.",
            $"'{nameof(TableRecord)}.{nameof(TableRecord.NamesADrop)}' is worked out from other fields and is not written to the file; it is not in the schema."
        ];

        /// <summary>Every reference the NPC parser resolves by id, addressed the way the file writes it.</summary>
        private static readonly (string Path, string Catalog)[] s_npcReferences =
        [
            ("abilities", DataCatalog.Abilities),
            ("stages.abilities", DataCatalog.Abilities),
            ("abilityBehaviors.id", DataCatalog.Abilities),
            ("reactions.abilityId", DataCatalog.Abilities),
            ("reactions.blockedByFinalDeathOf", DataCatalog.Npc),
            ("passives.id", DataCatalog.PassiveSkills),
        ];

        /// <summary>Every field the NPC parser turns into an enum member. The tool offers the members; a
        /// field the markup missed would take any word and fail at load instead.</summary>
        private static readonly (string Path, Type Members)[] s_npcChoices =
        [
            ("fraction", typeof(Fractions)),
            ("entityType", typeof(EntityType)),
            ("aiIntellect", typeof(AiIntellect)),
            ("stances", typeof(Stance)),
            ("rarity", typeof(Rarity)),
            ("authored.stance", typeof(Stance)),
            ("authored.rarity", typeof(Rarity)),
            ("lifecycle.kind", typeof(NpcLifecycleKind)),
            ("world.activity", typeof(WorldActivityType)),
            ("world.schedule.activity", typeof(WorldActivityType)),
            ("world.routine.activity", typeof(WorldActivityType)),
            ("abilityBehaviors.role", typeof(AbilityRole)),
            ("reactions.trigger", typeof(ReactionTrigger)),
            ("stages.attackEffects.effect", typeof(StageAttackEffectKind)),
        ];

        /// <summary>Every catalog a loot position may name its drop out of; any one of them knowing the
        /// id makes the position real, which is why the field carries them all at once.</summary>
        private static readonly string[] s_dropCatalogs =
        [
            DataCatalog.EquipItems,
            DataCatalog.Items,
            DataCatalog.Recipes,
            DataCatalog.Resources
        ];

        /// <summary>A catalog name the tool can neither describe nor knowingly skip is a catalog the
        /// author is silently locked out of, so both lists are held against the constants at once.</summary>
        [TestMethod]
        public void EveryCatalogConstantIsEitherDescribedOrNamedAsNotYet()
        {
            IReadOnlyList<string> catalogs = DataCatalogNames.All();
            Assert.IsTrue(catalogs.Count > 0, "no catalog names were found — the guard is checking nothing");

            List<string> described = [.. CatalogDescriptors.All.Select(descriptor => descriptor.Catalog)];
            List<string> pending = [.. CatalogDescriptors.NotYetDescribed];

            Assert.AreEqual(described.Count, described.Distinct(StringComparer.Ordinal).Count(), "a catalog is described twice");
            Assert.AreEqual(pending.Count, pending.Distinct(StringComparer.Ordinal).Count(), "a catalog is named as undescribed twice");

            List<string> both = [.. described.Intersect(pending, StringComparer.Ordinal).Order(StringComparer.Ordinal)];
            Assert.AreEqual(0, both.Count, $"catalogs both described and named as undescribed: {string.Join(", ", both)}");

            HashSet<string> claimed = [.. described, .. pending];
            List<string> unclaimed = [.. catalogs.Where(catalog => !claimed.Contains(catalog)).Order(StringComparer.Ordinal)];
            Assert.AreEqual(0, unclaimed.Count, $"catalog names on neither list: {string.Join(", ", unclaimed)}");

            List<string> strangers = [.. claimed.Where(catalog => !catalogs.Contains(catalog)).Order(StringComparer.Ordinal)];
            Assert.AreEqual(0, strangers.Count, $"named by the descriptors and unknown to {nameof(DataCatalog)}: {string.Join(", ", strangers)}");
        }

        /// <summary>The schema of the shipped catalog, built from the shipped DTOs. Both reports gather
        /// what neither reflection nor the assembled parts could vouch for, and an editor drawn from a
        /// schema with holes in it draws those holes as fields the author may not touch.</summary>
        [TestMethod]
        public void TheNpcSchemaBuildsFromTheRealDtoWithoutAReport()
        {
            CatalogSchemaBuilder builder = new(new SchemaReflector());
            CatalogSchema schema = builder.Build(Descriptor(DataCatalog.Npc));

            Assert.AreEqual(RootShape.ArrayUnderKey, schema.Shape);
            Assert.AreEqual(1, schema.Sections.Count);
            Assert.AreEqual(NpcCatalogDescriptor.RecordsKey, schema.Sections[0].Key);
            Assert.AreEqual(NpcCatalogDescriptor.IdField, schema.Sections[0].Record.IdField);
            CollectionAssert.AreEqual(
                new[] { LocalizedKeyAttribute.NoSuffix },
                schema.LocalizedSuffixes.ToArray(),
                "an npc is named in the localization by its own id and has no text of its own besides");

            CollectionAssert.AreEqual(
                s_allowedNpcNotes,
                builder.Reflection.Notes.ToArray(),
                $"reflection has something to say about the NPC DTOs:{Environment.NewLine}{builder.Reflection}");
            CollectionAssert.AreEqual(
                Array.Empty<string>(),
                builder.Checks.Notes.ToArray(),
                $"the assembled Npc catalog disagrees with itself:{Environment.NewLine}{builder.Checks}");
        }

        /// <summary>Every id the NPC parser resolves against another catalog, and every name it parses
        /// into an enum, said so in the schema. An unmarked one reads to the tool as free text: the
        /// author types a name nothing answers, and the miss surfaces at spawn.</summary>
        [TestMethod]
        public void TheNpcSchemaNamesTheReferencesTheParserResolves()
        {
            RecordSchema record = Schema(DataCatalog.Npc).Sections[0].Record;

            foreach ((string path, string catalog) in s_npcReferences)
            {
                FieldSchema field = Leaf(Locate(record, path));

                Assert.AreEqual(FieldKind.Reference, field.Kind, $"'{path}' is not a reference");
                CollectionAssert.Contains(field.RefCatalogs.ToArray(), catalog, $"'{path}' does not point into {catalog}");
            }

            foreach ((string path, Type members) in s_npcChoices)
            {
                FieldSchema field = Leaf(Locate(record, path));

                Assert.AreEqual(FieldKind.Enum, field.Kind, $"'{path}' is not a choice of names");
                CollectionAssert.AreEqual(Enum.GetNames(members), field.EnumValues.ToArray(), $"'{path}' offers other members than {members.Name}");
            }
        }

        /// <summary>
        /// The shipped file read back through the schema: every key it holds is a key the schema ranks,
        /// and what the canonical write puts out holds everything the file held. The keys of a map the
        /// author fills himself are the exception, and the schema itself names them: nothing in the
        /// contract marks the keys of a dictionary, so the tool carries them through as the file had them.
        /// <para>Where the shipped file wrote the keys of a record in another order the writing IS the
        /// change — the policy is canonicalizing — so the records it would move are reported and not
        /// failed.</para>
        /// </summary>
        [TestMethod]
        public void TheNpcSchemaRanksEveryKeyTheShippedFileWrites()
        {
            CatalogSchema schema = Schema(DataCatalog.Npc);
            JToken root = JsonTreeDocument.Load(CatalogFile(schema, DataCatalog.Npc)).Root;
            SchemaKeyOrder order = new(schema);

            (List<string> unknown, List<string> reordered) = Written(root, schema, order);

            Report("Keys of Npc.json the schema does not know", unknown);
            Report("Records the canonical write would reorder", reordered);

            Assert.AreEqual(0, unknown.Count,
                $"keys of Npc.json no field of the schema is written under: {string.Join(", ", unknown)}");
            Assert.IsTrue(
                JToken.DeepEquals(root, JsonTreeDocument.Parse(CanonicalJsonWriter.Write(root, order, CanonicalJsonOptions.Default)).Root),
                "the canonical write of Npc.json says something other than the file it was given");
        }

        /// <summary>
        /// The schema of the loot tables, built from the shipped DTOs. Unlike the plain catalogs these
        /// DTOs do have something to say for themselves — a converter and two positional records — and
        /// each note is named rather than counted: a walk that reports four things is only proof of
        /// anything while those four are the four it is expected to report.
        /// </summary>
        [TestMethod]
        public void TheLootTablesSchemaBuildsFromTheRealDtosWithTheNotesItsPositionsAreKnownFor()
        {
            CatalogSchemaBuilder builder = new(new SchemaReflector());
            CatalogSchema schema = builder.Build(Descriptor(DataCatalog.LootTables));

            Assert.AreEqual(RootShape.SectionsOfArrays, schema.Shape);
            CollectionAssert.AreEqual(
                new[]
                {
                    LootTablesCatalogDescriptor.GeneralKey,
                    LootTablesCatalogDescriptor.FractionsKey,
                    LootTablesCatalogDescriptor.TypesKey,
                    LootTablesCatalogDescriptor.IndividualKey
                },
                schema.Sections.Select(section => section.Key).ToArray());
            Assert.AreEqual(0, schema.LocalizedSuffixes.Count, "a table carries no text of its own: what drops is named in its own catalog");
            Assert.AreEqual(LootTablesCatalogDescriptor.FileName, schema.Placement.FileFor(_ => null));

            CollectionAssert.AreEquivalent(
                s_allowedLootTableNotes,
                builder.Reflection.Notes.ToArray(),
                $"reflection has something else to say about the loot table DTOs:{Environment.NewLine}{builder.Reflection}");
            CollectionAssert.AreEqual(
                Array.Empty<string>(),
                builder.Checks.Notes.ToArray(),
                $"the assembled LootTables catalog disagrees with itself:{Environment.NewLine}{builder.Checks}");
        }

        /// <summary>
        /// A position takes one of two shapes, and the tool has to find both of them two arrays below
        /// every one of the four sections — that is what registering the shapes against the DTO buys.
        /// Which shape stands in a file is told by the key that is there, not by a value: a position
        /// naming a thing writes an id, a position naming a set of augments writes the filter.
        /// </summary>
        [TestMethod]
        public void TheLootTablesSchemaCarriesBothShapesOfAPositionIntoEverySection()
        {
            CatalogSchema schema = Schema(DataCatalog.LootTables);

            foreach (SectionSchema section in schema.Sections)
            {
                RecordSchema position = Position(section.Record);
                Assert.AreEqual(nameof(TableRecord), position.TypeName);
                Assert.IsNull(position.IdField, "a seat at a table is not a record anything names");

                VariantSet shapes = position.Variants
                                    ?? throw new AssertFailedException($"the positions under '{section.Key}' take one shape only");

                Assert.IsNull(shapes.Discriminator, "the shapes are told apart by the key that is there, not by a value");
                CollectionAssert.AreEqual(
                    new[] { LootTablesCatalogDescriptor.IdField, LootTablesCatalogDescriptor.AugmentsField },
                    shapes.Variants.Select(variant => variant.DiscriminatorValue).ToArray());

                RecordSchema named = Shape(shapes, LootTablesCatalogDescriptor.IdField);
                RecordSchema grouped = Shape(shapes, LootTablesCatalogDescriptor.AugmentsField);
                Assert.AreEqual(nameof(LootPositionById), named.TypeName);
                Assert.AreEqual(nameof(LootPositionByGroup), grouped.TypeName);

                NamesADrop(Field(named, LootTablesCatalogDescriptor.IdField), $"{section.Key}: the shape naming one thing");
                NamesADrop(Field(position, LootTablesCatalogDescriptor.IdField), $"{section.Key}: the position itself");

                RecordSchema group = Nested(grouped, LootTablesCatalogDescriptor.AugmentsField);
                Assert.AreEqual(nameof(AugmentGroup), group.TypeName);
                Assert.AreEqual(FieldKind.Integer, Field(group, TierField).Kind);
                Assert.AreEqual(FieldKind.Enum, Field(group, RarityField).Kind);
                CollectionAssert.AreEqual(Enum.GetNames<Rarity>(), Field(group, RarityField).EnumValues.ToArray());
            }
        }

        /// <summary>
        /// What a table's key names is the section's answer and not the type's: one DTO stands under all
        /// four sections, so a key is a fraction here, a kind of foe there, an npc's own id in the third
        /// — and in the fourth a name the author picks, which is said out loud rather than left silent.
        /// </summary>
        [TestMethod]
        public void TheLootTablesSchemaNamesWhatEachSectionsKeyPointsAt()
        {
            CatalogSchema schema = Schema(DataCatalog.LootTables);

            foreach (SectionSchema section in schema.Sections)
                Assert.AreEqual(LootTablesCatalogDescriptor.KeyField, section.Record.IdField, $"a table of '{section.Key}' is found by another field");

            FieldSchema general = SectionKey(schema, LootTablesCatalogDescriptor.GeneralKey);
            Assert.AreEqual(FieldKind.String, general.Kind);
            Assert.IsTrue(general.RefusedAsReference, "the key of a general table points at nothing, and nothing says so");

            Choice(SectionKey(schema, LootTablesCatalogDescriptor.FractionsKey), typeof(Fractions));
            Choice(SectionKey(schema, LootTablesCatalogDescriptor.TypesKey), typeof(EntityType));

            FieldSchema individual = SectionKey(schema, LootTablesCatalogDescriptor.IndividualKey);
            Assert.AreEqual(FieldKind.Reference, individual.Kind);
            CollectionAssert.AreEqual(new[] { DataCatalog.Npc }, individual.RefCatalogs.ToArray());
        }

        /// <summary>The shipped file read back through the schema, the way the NPC one is: every key it
        /// writes is one the schema ranks, and the canonical write loses nothing. No map stands anywhere
        /// in these tables, so the exception made for keys the author fills himself is checked to be
        /// empty rather than listed — a map added later would go unranked and is a failure here.</summary>
        [TestMethod]
        public void TheLootTablesSchemaRanksEveryKeyTheShippedFileWrites()
        {
            CatalogSchema schema = Schema(DataCatalog.LootTables);
            JToken root = JsonTreeDocument.Load(CatalogFile(schema, DataCatalog.LootTables)).Root;
            SchemaKeyOrder order = new(schema);

            List<string> maps = [.. Records(schema).SelectMany(record => record.Fields).Where(field => Map(field) != null).Select(field => field.JsonName)];
            Assert.AreEqual(0, maps.Count, $"the loot tables hold maps, whose keys nothing ranks: {string.Join(", ", maps)}");

            (List<string> unknown, List<string> reordered) = Written(root, schema, order);

            Report("Keys of LootTables.json the schema does not know", unknown);
            Report("Records the canonical write would reorder", reordered);

            Assert.AreEqual(0, unknown.Count,
                $"keys of LootTables.json no field of the schema is written under: {string.Join(", ", unknown)}");
            Assert.IsTrue(
                JToken.DeepEquals(root, JsonTreeDocument.Parse(CanonicalJsonWriter.Write(root, order, CanonicalJsonOptions.Default)).Root),
                "the canonical write of LootTables.json says something other than the file it was given");
        }

        /// <summary>
        /// Across every described catalog: a field written under a name ending in "id" either points into
        /// a catalog or says out loud that it does not. The one exception is the field a record IS found
        /// by, which names its own record rather than another.
        /// <para>Keys of a map are not covered — the contract has no way of refusing them — so a map keyed
        /// by ids is still a silence this cannot see.</para>
        /// </summary>
        [TestMethod]
        public void EveryFieldWrittenUnderAnIdNameIsAReferenceOrRefusesToBeOne()
        {
            List<string> silent = [];

            foreach (ICatalogDescriptor descriptor in CatalogDescriptors.All)
                foreach (RecordSchema record in Records(new CatalogSchemaBuilder(new SchemaReflector()).Build(descriptor)))
                    foreach (FieldSchema field in record.Fields)
                    {
                        if (!field.JsonName.EndsWith(IdSuffix, StringComparison.OrdinalIgnoreCase)) continue;
                        if (string.Equals(field.JsonName, record.IdField, StringComparison.Ordinal)) continue;

                        FieldSchema leaf = Leaf(field);
                        if (leaf.Kind == FieldKind.Reference || leaf.RefusedAsReference) continue;

                        silent.Add($"{descriptor.Catalog}: {record.TypeName}.{field.JsonName}");
                    }

            Assert.AreEqual(0, silent.Count,
                $"fields named like an id that neither point anywhere nor say they do not: {string.Join(", ", silent.Distinct().Order(StringComparer.Ordinal))}");
        }

        /// <summary>The one descriptor of a catalog, taken from the registry the tool reads: a schema
        /// built from a descriptor the registry does not hold would pin nothing the tool uses.</summary>
        private static ICatalogDescriptor Descriptor(string catalog) =>
            CatalogDescriptors.All.FirstOrDefault(descriptor => descriptor.Catalog == catalog)
            ?? throw new AssertFailedException($"{nameof(CatalogDescriptors)} holds no descriptor of the {catalog} catalog.");

        private static CatalogSchema Schema(string catalog) => new CatalogSchemaBuilder(new SchemaReflector()).Build(Descriptor(catalog));

        /// <summary>The shipped file of a catalog, named by the schema's own placement rule.</summary>
        private static string CatalogFile(CatalogSchema schema, string catalog)
        {
            string name = schema.Placement.FileFor(_ => null)
                          ?? throw new AssertFailedException($"the {catalog} catalog names no file of its own.");

            return Path.Combine(SharedData.Catalog(catalog), name + JsonExtension);
        }

        /// <summary>What a shipped file writes against what the schema knows: the keys no field is
        /// written under, and the records the canonical write would put in another order.</summary>
        private static (List<string> Unknown, List<string> Reordered) Written(JToken root, CatalogSchema schema, SchemaKeyOrder order)
        {
            HashSet<string> freeMaps = FreeKeyedMaps(schema);
            List<string> unknown = [];
            List<string> reordered = [];

            foreach ((JsonPointer at, JObject holder) in Objects(root, JsonPointer.Root))
            {
                List<string> written = [.. holder.Properties().Select(property => property.Name)];
                List<string> ranked = [.. written.OrderBy(key => order.Rank(at, key))];

                if (!written.SequenceEqual(ranked, StringComparer.Ordinal))
                    reordered.Add($"{at}: {string.Join(", ", written)} → {string.Join(", ", ranked)}");

                if (freeMaps.Contains(at.Last ?? string.Empty)) continue;

                unknown.AddRange(written
                    .Where(key => order.Rank(at, key) == IKeyOrder.Unknown)
                    .Select(key => $"{at}/{key}"));
            }

            return (unknown, reordered);
        }

        /// <summary>The field one path addresses, stepping through whatever lists and maps hold the
        /// records on the way.</summary>
        private static FieldSchema Locate(RecordSchema record, string path)
        {
            RecordSchema level = record;
            string[] segments = path.Split(PathSeparator);

            for (int segment = 0; segment < segments.Length; segment++)
            {
                FieldSchema field = level.Fields.FirstOrDefault(candidate => candidate.JsonName == segments[segment])
                                    ?? throw new AssertFailedException($"'{level.TypeName}' writes no '{segments[segment]}' (looking for '{path}').");

                if (segment == segments.Length - 1) return field;

                level = Leaf(field).Record
                        ?? throw new AssertFailedException($"'{path}' walks through '{segments[segment]}', which holds no records.");
            }

            throw new AssertFailedException($"'{path}' addresses nothing.");
        }

        private static FieldSchema Field(RecordSchema record, string jsonName) =>
            record.Fields.FirstOrDefault(field => field.JsonName == jsonName)
            ?? throw new AssertFailedException($"'{record.TypeName}' writes no '{jsonName}'.");

        /// <summary>The record a field holds one of.</summary>
        private static RecordSchema Nested(RecordSchema record, string jsonName) =>
            Leaf(Field(record, jsonName)).Record
            ?? throw new AssertFailedException($"'{record.TypeName}.{jsonName}' holds no records.");

        /// <summary>The seats of a table: two arrays down, whichever section the table stands in.</summary>
        private static RecordSchema Position(RecordSchema table) => Nested(Nested(table, TiersField), ItemsField);

        private static RecordSchema Shape(VariantSet shapes, string key) =>
            shapes.Variants.FirstOrDefault(variant => variant.DiscriminatorValue == key)?.Record
            ?? throw new AssertFailedException($"no shape is picked by '{key}'.");

        private static FieldSchema SectionKey(CatalogSchema schema, string section) =>
            Field(
                schema.Sections.FirstOrDefault(candidate => candidate.Key == section)?.Record
                ?? throw new AssertFailedException($"the catalog holds no '{section}' section."),
                LootTablesCatalogDescriptor.KeyField);

        /// <summary>That a field names something to drop, out of every catalog droppable things live in.</summary>
        private static void NamesADrop(FieldSchema field, string what)
        {
            Assert.AreEqual(FieldKind.Reference, field.Kind, $"{what} does not name anything");
            Assert.IsFalse(field.AllowEmpty, $"{what} may be left empty, which prices a seat that drops nothing");
            CollectionAssert.AreEquivalent(s_dropCatalogs, field.RefCatalogs.ToArray(), $"{what} points into other catalogs");
        }

        private static void Choice(FieldSchema field, Type members)
        {
            Assert.AreEqual(FieldKind.Enum, field.Kind, $"'{field.JsonName}' is not a choice of names");
            CollectionAssert.AreEqual(Enum.GetNames(members), field.EnumValues.ToArray(), $"'{field.JsonName}' offers other members than {members.Name}");
        }

        /// <summary>What a field ultimately holds: the element of a list, the value of a map, or the
        /// field itself. Markup travels to the value, so that is where a reference or a choice is said.</summary>
        private static FieldSchema Leaf(FieldSchema field)
        {
            FieldSchema leaf = field;

            while (leaf.Kind is FieldKind.Array or FieldKind.Dictionary && leaf.Item is { } item) leaf = item;

            return leaf;
        }

        /// <summary>The map a field holds, if it holds one: the field itself, or the element of the list
        /// it is. Null when nothing under the field is a map.</summary>
        private static FieldSchema? Map(FieldSchema field)
        {
            for (FieldSchema? node = field; node != null; node = node.Item)
                if (node.Kind == FieldKind.Dictionary) return node;

            return null;
        }

        /// <summary>Json names of the maps whose keys the author writes freely. Read off the schema
        /// rather than listed here: nothing in the contract marks the keys of a dictionary, so a map is
        /// recognised by the schema having no key schema for it.</summary>
        private static HashSet<string> FreeKeyedMaps(CatalogSchema schema) =>
        [
            .. Records(schema)
                .SelectMany(record => record.Fields)
                .Where(field => Map(field) is { Key: null })
                .Select(field => field.JsonName)
        ];

        /// <summary>Every record the catalog reaches: those of its sections, the shapes they take, and
        /// everything nested in their fields. A field leading back to a record already being read carries
        /// no record of its own, so the walk always ends.</summary>
        private static IEnumerable<RecordSchema> Records(CatalogSchema schema)
        {
            HashSet<RecordSchema> seen = new(ReferenceEqualityComparer.Instance);
            Stack<RecordSchema> pending = new(schema.Sections.Select(section => section.Record));

            while (pending.Count > 0)
            {
                RecordSchema record = pending.Pop();

                if (!seen.Add(record)) continue;

                yield return record;

                if (record.Variants is { } variants)
                    foreach (VariantSchema variant in variants.Variants)
                        pending.Push(variant.Record);

                foreach (FieldSchema field in record.Fields)
                    foreach (RecordSchema nested in Held(field))
                        pending.Push(nested);
            }
        }

        /// <summary>The records standing under one field, whatever lists and maps hold them.</summary>
        private static IEnumerable<RecordSchema> Held(FieldSchema field)
        {
            if (field.Record is { } record) yield return record;

            if (field.Item is { } item)
                foreach (RecordSchema nested in Held(item))
                    yield return nested;

            if (field.Key is { } key)
                foreach (RecordSchema nested in Held(key))
                    yield return nested;
        }

        /// <summary>Every object of a document with the address it sits at — the address the key order
        /// is asked about.</summary>
        private static IEnumerable<(JsonPointer At, JObject Holder)> Objects(JToken token, JsonPointer at)
        {
            switch (token)
            {
                case JObject holder:
                    yield return (at, holder);

                    foreach (JProperty property in holder.Properties())
                        foreach ((JsonPointer, JObject) nested in Objects(property.Value, at.Append(property.Name)))
                            yield return nested;

                    break;

                case JArray array:
                    for (int index = 0; index < array.Count; index++)
                        foreach ((JsonPointer, JObject) nested in Objects(array[index], at.Append(index)))
                            yield return nested;

                    break;
            }
        }

        /// <summary>What the run has to say about the shipped file — a finding for whoever owns the data,
        /// never a failure: neither list is something the schema can put right.</summary>
        private static void Report(string what, List<string> lines) =>
            Console.WriteLine(lines.Count == 0
                ? $"{what}: none."
                : $"{what} ({lines.Count}):{Environment.NewLine}  {string.Join($"{Environment.NewLine}  ", lines)}");
    }
}

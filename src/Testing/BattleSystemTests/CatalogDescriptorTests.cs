namespace LastBreathTest.BattleSystemTests
{
    using System.Reflection;
    using Core.Ai;
    using Core.Ai.World;
    using Core.Data.GameData;
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
    /// tool can be handed a schema for, and the schema of the first of them is built here from the real
    /// DTOs against the real shipped file.
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

        /// <summary>What the reflector may still have to say about the real NPC DTOs. Empty: everything
        /// the walk meets there is a plain property it can read whole. A note appearing here is either a
        /// DTO to fix or a fact to write down — never something to silence by widening the check.</summary>
        private static readonly string[] s_allowedReflectionNotes = [];

        /// <summary>Every reference the parser resolves by id, addressed the way the file writes it.</summary>
        private static readonly (string Path, string Catalog)[] s_references =
        [
            ("abilities", DataCatalog.Abilities),
            ("stages.abilities", DataCatalog.Abilities),
            ("abilityBehaviors.id", DataCatalog.Abilities),
            ("reactions.abilityId", DataCatalog.Abilities),
            ("reactions.blockedByFinalDeathOf", DataCatalog.Npc),
            ("passives.id", DataCatalog.PassiveSkills),
        ];

        /// <summary>Every field the parser turns into an enum member. The tool offers the members; a
        /// field the markup missed would take any word and fail at load instead.</summary>
        private static readonly (string Path, Type Members)[] s_choices =
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

        /// <summary>A catalog name the tool can neither describe nor knowingly skip is a catalog the
        /// author is silently locked out of, so both lists are held against the constants at once.</summary>
        [TestMethod]
        public void EveryCatalogConstantIsEitherDescribedOrNamedAsNotYet()
        {
            List<string> catalogs = CatalogNames();
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
            CatalogSchema schema = builder.Build(NpcDescriptor());

            Assert.AreEqual(RootShape.ArrayUnderKey, schema.Shape);
            Assert.AreEqual(NpcCatalogDescriptor.RecordsKey, schema.Sections[0].Key);
            Assert.AreEqual(NpcCatalogDescriptor.IdField, schema.Sections[0].Record.IdField);

            CollectionAssert.AreEqual(
                s_allowedReflectionNotes,
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
            RecordSchema record = NpcSchema().Sections[0].Record;

            foreach ((string path, string catalog) in s_references)
            {
                FieldSchema field = Leaf(Locate(record, path));

                Assert.AreEqual(FieldKind.Reference, field.Kind, $"'{path}' is not a reference");
                CollectionAssert.Contains(field.RefCatalogs.ToArray(), catalog, $"'{path}' does not point into {catalog}");
            }

            foreach ((string path, Type members) in s_choices)
            {
                FieldSchema field = Leaf(Locate(record, path));

                Assert.AreEqual(FieldKind.Enum, field.Kind, $"'{path}' is not a choice of names");
                CollectionAssert.AreEqual(Enum.GetNames(members), field.EnumValues.ToArray(), $"'{path}' offers other members than {members.Name}");
            }
        }

        /// <summary>
        /// The shipped file read back through the schema: every key it holds is a key the schema ranks,
        /// and the order the schema puts them in is one the file settles into — writing it twice writes
        /// the same thing. The keys of a map the author fills himself are the exception, and the schema
        /// itself names them: nothing in the contract marks the keys of a dictionary, so the tool carries
        /// them through in the order the file had.
        /// <para>Where the shipped file wrote the keys of a record in another order the writing IS the
        /// change — the policy is canonicalizing — so the records it would move are reported and not
        /// failed.</para>
        /// </summary>
        [TestMethod]
        public void TheNpcSchemaRanksEveryKeyTheShippedFileWrites()
        {
            CatalogSchema schema = NpcSchema();
            JToken root = JsonTreeDocument.Load(NpcFile(schema)).Root;
            SchemaKeyOrder order = new(schema);
            HashSet<string> freeMaps = FreeKeyedMaps(schema);

            List<string> reordered = [];
            List<string> unknown = [];

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

            Report("Keys of Npc.json the schema does not know", unknown);
            Report("Records the canonical write would reorder", reordered);

            string once = CanonicalJsonWriter.Write(root, order, CanonicalJsonOptions.Default);
            string twice = CanonicalJsonWriter.Write(JsonTreeDocument.Parse(once).Root, order, CanonicalJsonOptions.Default);

            Assert.AreEqual(0, unknown.Count,
                $"keys of Npc.json no field of the schema is written under: {string.Join(", ", unknown)}");
            Assert.AreEqual(once, twice, "the schema orders the file into a place it does not stay in");
        }

        /// <summary>The one descriptor of the catalog, taken from the registry the tool reads: a schema
        /// built from a descriptor the registry does not hold would pin nothing the tool uses.</summary>
        private static ICatalogDescriptor NpcDescriptor() =>
            CatalogDescriptors.All.FirstOrDefault(descriptor => descriptor.Catalog == DataCatalog.Npc)
            ?? throw new AssertFailedException($"{nameof(CatalogDescriptors)} holds no descriptor of the {DataCatalog.Npc} catalog.");

        private static CatalogSchema NpcSchema() => new CatalogSchemaBuilder(new SchemaReflector()).Build(NpcDescriptor());

        /// <summary>The shipped file of the catalog, named by the schema's own placement rule.</summary>
        private static string NpcFile(CatalogSchema schema)
        {
            string name = schema.Placement.FileFor(_ => null)
                          ?? throw new AssertFailedException($"the {DataCatalog.Npc} catalog names no file of its own.");

            return Path.Combine(SharedData.Catalog(DataCatalog.Npc), name + JsonExtension);
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

        /// <summary>What a field ultimately holds: the element of a list, the value of a map, or the
        /// field itself. Markup travels to the value, so that is where a reference or a choice is said.</summary>
        private static FieldSchema Leaf(FieldSchema field)
        {
            FieldSchema leaf = field;

            while (leaf.Kind is FieldKind.Array or FieldKind.Dictionary && leaf.Item is { } item) leaf = item;

            return leaf;
        }

        /// <summary>Json names of the maps whose keys the author writes freely. Read off the schema
        /// rather than listed here: nothing in the contract marks the keys of a dictionary, so a map is
        /// recognised by the schema having no key schema for it.</summary>
        private static HashSet<string> FreeKeyedMaps(CatalogSchema schema) =>
        [
            .. Fields(schema.Sections[0].Record)
                .Where(field => field is { Kind: FieldKind.Dictionary, Key: null })
                .Select(field => field.JsonName)
        ];

        /// <summary>Every field the record reaches, its own and those of everything nested in it.</summary>
        private static IEnumerable<FieldSchema> Fields(RecordSchema record)
        {
            foreach (FieldSchema field in record.Fields)
            {
                yield return field;

                RecordSchema? nested = Leaf(field).Record;
                if (nested == null) continue;

                foreach (FieldSchema inner in Fields(nested)) yield return inner;
            }
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

        private static List<string> CatalogNames() =>
        [
            .. typeof(DataCatalog)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => field is { IsLiteral: true, IsInitOnly: false } && field.FieldType == typeof(string))
                .Select(field => (string)field.GetRawConstantValue()!)
        ];
    }
}

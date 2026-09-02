namespace Tooling.Tests.Schema
{
    using System;
    using System.Linq;
    using Newtonsoft.Json.Linq;
    using Tooling.Json;
    using Tooling.Schema;
    using Tooling.Schema.Model;
    using Tooling.Schema.Reflection;
    using static Tooling.Tests.Schema.SchemaReflectorTests;

    /// <summary>
    /// The order a catalog's file is written in. The schema is the only thing that knows it — the writer
    /// sorts by an answer it is given — so what is pinned here is that the answer is the DTO's own order of
    /// declaration, at every depth a record sits at, and that anything the schema cannot place is left
    /// exactly where the file had it.
    /// </summary>
    [TestClass]
    public class SchemaKeyOrderTests
    {
        private const string NpcsKey = "npcs";
        private const string GeneralKey = "general";
        private const string IndividualKey = "individual";

        private const string IdField = "id";
        private const string PriceField = "price";
        private const string AugmentsField = "augments";
        private const string TierField = "tier";
        private const string RarityField = "rarity";
        private const string NameField = "name";
        private const string LevelField = "level";
        private const string ScalingField = "levelScaling";
        private const string AuthoredField = "authored";
        private const string BaseParametersField = "baseParameters";
        private const string HealthKey = "Health";
        private const string UnheardOf = "unheardOf";

        private const string KindField = "kind";
        private const string AlphaField = "alpha";
        private const string BetaField = "beta";

        private const string LeftKind = "left";
        private const string RightKind = "right";

        private const string AuthoredType = "Authored";
        private const string ShapedType = "Shaped";

        private const string NpcPointer = "/npcs/0";
        private const string AuthoredPointer = "/npcs/0/authored";
        private const string ParametersPointer = "/npcs/0/baseParameters";
        private const string ConditionsPointer = "/npcs/0/acceptConditions";
        private const string PositionPointer = "/general/0/tiers/0/items/0";
        private const string GroupPointer = "/general/0/tiers/0/items/0/augments";
        private const string NoSuchPointer = "/npcs/0/abilities/0/deeper";
        private const string NotAnIndexPointer = "/npcs/first";

        private const string RonaldId = "Npc_Ronald";

        /// <summary>A file whose keys stand in every order but the right one, with one key no build of the
        /// game has ever heard of.</summary>
        private const string Scrambled = """
            {
                "npcs": [
                    {
                        "levelScaling": 0.5,
                        "unheardOf": true,
                        "id": "Npc_Ronald",
                        "authored": {
                            "name": "Ronald",
                            "level": 3
                        }
                    }
                ]
            }
            """;

        [TestMethod]
        public void TheKeysOfTheRoot_AreTheSectionsInTheOrderTheCatalogNamesThem()
        {
            SchemaKeyOrder order = new(Loot());

            Assert.AreEqual(0, order.Rank(JsonPointer.Root, GeneralKey));
            Assert.AreEqual(1, order.Rank(JsonPointer.Root, IndividualKey));
            Assert.AreEqual(IKeyOrder.Unknown, order.Rank(JsonPointer.Root, NpcsKey));
        }

        [TestMethod]
        public void TheKeysOfARecord_StandWhereTheDtoDeclaresThem()
        {
            SchemaKeyOrder order = new(Npcs());
            JsonPointer npc = JsonPointer.Parse(NpcPointer);

            Assert.AreEqual(0, order.Rank(npc, IdField));
            Assert.AreEqual(9, order.Rank(npc, ScalingField));
            Assert.IsTrue(order.Rank(npc, AuthoredField) < order.Rank(npc, ScalingField));
        }

        [TestMethod]
        public void ANestedRecord_IsRankedByItsOwnFields()
        {
            SchemaKeyOrder order = new(Npcs());
            JsonPointer authored = JsonPointer.Parse(AuthoredPointer);

            Assert.AreEqual(0, order.Rank(authored, LevelField));
            Assert.AreEqual(1, order.Rank(authored, NameField));
        }

        /// <summary>The tail is where a key this build does not know goes, which is what carries a newer
        /// file's fields through a save with their order intact.</summary>
        [TestMethod]
        public void AKeyTheRecordDoesNotDeclare_IsUnknown() =>
            Assert.AreEqual(IKeyOrder.Unknown, new SchemaKeyOrder(Npcs()).Rank(JsonPointer.Parse(NpcPointer), UnheardOf));

        /// <summary>The author names the keys of a free map, so the schema has nothing to say about their
        /// order and says exactly that.</summary>
        [TestMethod]
        public void TheKeysOfAFreeMap_AreUnknown() =>
            Assert.AreEqual(IKeyOrder.Unknown, new SchemaKeyOrder(Npcs()).Rank(JsonPointer.Parse(ParametersPointer), HealthKey));

        [TestMethod]
        public void APathTheSchemaCannotWalk_LeavesEveryKeyUnknown()
        {
            SchemaKeyOrder order = new(Npcs());

            Assert.AreEqual(IKeyOrder.Unknown, order.Rank(JsonPointer.Parse(NoSuchPointer), IdField));
            Assert.AreEqual(IKeyOrder.Unknown, order.Rank(JsonPointer.Parse(NotAnIndexPointer), IdField), "an array is addressed by an index");
        }

        /// <summary>A position sits two arrays below its section and takes two shapes. The writer is asked
        /// about a key and not about a value, so every shape's fields are placed at once — the first shape
        /// to write a name owns its place, and the record's own fields come after them.</summary>
        [TestMethod]
        public void ARecordTakingSeveralShapes_IsRankedByAllOfThem()
        {
            SchemaKeyOrder order = new(Loot());
            JsonPointer position = JsonPointer.Parse(PositionPointer);

            Assert.AreEqual(0, order.Rank(position, IdField));
            Assert.AreEqual(1, order.Rank(position, PriceField));
            Assert.AreEqual(2, order.Rank(position, AugmentsField));
            Assert.AreEqual(IKeyOrder.Unknown, order.Rank(position, UnheardOf));
        }

        /// <summary>And the walk goes on through a field only one of the shapes has.</summary>
        [TestMethod]
        public void TheWalkGoesOnThroughAFieldOnlyOneShapeHas()
        {
            SchemaKeyOrder order = new(Loot());
            JsonPointer group = JsonPointer.Parse(GroupPointer);

            Assert.AreEqual(0, order.Rank(group, TierField));
            Assert.AreEqual(1, order.Rank(group, RarityField));
        }

        /// <summary>A free structure is kept verbatim, so nothing inside it has a place the schema knows.</summary>
        [TestMethod]
        public void TheKeysInsideAFreeStructure_AreUnknown() =>
            Assert.AreEqual(IKeyOrder.Unknown, new SchemaKeyOrder(Npcs()).Rank(JsonPointer.Parse(ConditionsPointer), IdField));

        /// <summary>Two shapes writing different things under one name: the name keeps its place, and the
        /// walk stops there. Going on through the first shape would rank one shape's insides by the other's
        /// fields and move lines the author never touched.</summary>
        [TestMethod]
        public void OneNameTwoShapesWriteDifferentThingsUnder_StopsTheWalk()
        {
            SchemaKeyOrder order = new(Shaped(Authored(AlphaField), Authored(BetaField)));

            Assert.AreEqual(1, order.Rank(JsonPointer.Parse(NpcPointer), AuthoredField), "the name keeps its place");
            Assert.AreEqual(IKeyOrder.Unknown, order.Rank(JsonPointer.Parse(AuthoredPointer), AlphaField));
            Assert.AreEqual(IKeyOrder.Unknown, order.Rank(JsonPointer.Parse(AuthoredPointer), BetaField));
        }

        /// <summary>And two shapes writing the SAME thing under one name are one thing, walked through.</summary>
        [TestMethod]
        public void OneNameTwoShapesWriteTheSameThingUnder_IsWalkedThrough()
        {
            SchemaKeyOrder order = new(Shaped(Authored(AlphaField), Authored(AlphaField)));

            Assert.AreEqual(0, order.Rank(JsonPointer.Parse(AuthoredPointer), AlphaField));
        }

        /// <summary>A catalog that is a map of id to record: the ids are the author's, the records are the
        /// schema's.</summary>
        [TestMethod]
        public void ACatalogKeyedById_RanksTheRecordsAndNotTheIds()
        {
            SchemaKeyOrder order = new(Keyed());

            Assert.AreEqual(IKeyOrder.Unknown, order.Rank(JsonPointer.Root, RonaldId));
            Assert.AreEqual(0, order.Rank(JsonPointer.Root.Append(RonaldId), LevelField));
        }

        /// <summary>A settings document is one record, and the root is it.</summary>
        [TestMethod]
        public void ACatalogOfOneRecord_RanksTheRootItself()
        {
            SchemaKeyOrder order = new(Single());

            Assert.AreEqual(0, order.Rank(JsonPointer.Root, LevelField));
            Assert.AreEqual(1, order.Rank(JsonPointer.Root, NameField));
        }

        /// <summary>A section with no key of its own is the file: the records are the root array.</summary>
        [TestMethod]
        public void ACatalogWhoseRecordsAreTheRoot_RanksThemUnderTheirIndex()
        {
            SchemaKeyOrder order = new(Bare());

            Assert.AreEqual(IKeyOrder.Unknown, order.Rank(JsonPointer.Root, LevelField));
            Assert.AreEqual(0, order.Rank(JsonPointer.Root.Append(0), LevelField));
        }

        /// <summary>The whole point of the thing: a file whose keys stand in any order comes back written in
        /// the order the DTOs declare, at every depth, with the key nobody knows kept at the tail.</summary>
        [TestMethod]
        public void TheWriter_PutsAFilesKeysInTheOrderTheDtosDeclareThem()
        {
            string written = JsonTreeDocument.Parse(Scrambled).Write(new SchemaKeyOrder(Npcs()));
            JObject root = (JObject)JToken.Parse(written);
            var npcs = (JArray)root[NpcsKey]!;
            var npc = (JObject)npcs[0];
            var authored = (JObject)npc[AuthoredField]!;

            CollectionAssert.AreEqual(new[] { IdField, AuthoredField, ScalingField, UnheardOf }, Keys(npc));
            CollectionAssert.AreEqual(new[] { LevelField, NameField }, Keys(authored));
        }

        [TestMethod]
        public void TheOrder_RefusesNothingWhereSomethingIsRequired()
        {
            SchemaKeyOrder order = new(Npcs());

            Assert.ThrowsException<ArgumentNullException>(() => new SchemaKeyOrder(null!));
            Assert.ThrowsException<ArgumentNullException>(() => order.Rank(null!, IdField));
            Assert.ThrowsException<ArgumentNullException>(() => order.Rank(JsonPointer.Root, null!));
        }

        private static string[] Keys(JObject holder) => [.. holder.Properties().Select(property => property.Name)];

        private static FieldSchema Text(string jsonName) => new() { JsonName = jsonName, Kind = FieldKind.String };

        private static FieldSchema Holder(string jsonName, RecordSchema record) =>
            new() { JsonName = jsonName, Kind = FieldKind.Object, Record = record };

        /// <summary>The nested record one shape writes under "authored" — one type name, so that two of
        /// them holding the same field are the same record and not merely a similar one.</summary>
        private static RecordSchema Authored(string jsonName) =>
            new() { TypeName = AuthoredType, Fields = [Text(jsonName)] };

        /// <summary>A catalog of records taking two shapes, each with its own idea of what stands under
        /// "authored".</summary>
        private static CatalogSchema Shaped(RecordSchema left, RecordSchema right)
        {
            RecordSchema Shape(string kind, RecordSchema authored) => new()
            {
                TypeName = kind,
                Fields = [Text(KindField), Holder(AuthoredField, authored)]
            };

            VariantSet shapes = new(
                [
                    SchemaReflectorTests.Variant(LeftKind, Shape(LeftKind, left)),
                    SchemaReflectorTests.Variant(RightKind, Shape(RightKind, right))
                ],
                KindField);

            RecordSchema record = new() { TypeName = ShapedType, Fields = [Text(KindField)], Variants = shapes };

            return new CatalogSchema(RootShape.ArrayUnderKey, [Section(NpcsKey, record)], [], new FreeFilePlacement());
        }

        private static CatalogSchema Npcs() => new CatalogSchemaBuilder().Build(new NpcsDescriptor());

        private static CatalogSchema Loot() => new CatalogSchemaBuilder().Build(new LootDescriptor());

        private static CatalogSchema Keyed() => new CatalogSchemaBuilder().Build(new KeyedDescriptor());

        private static CatalogSchema Single() => new CatalogSchemaBuilder().Build(new SingleDescriptor());

        private static CatalogSchema Bare() => new CatalogSchemaBuilder().Build(new BareDescriptor());

        private sealed class NpcsDescriptor : ICatalogDescriptor
        {
            public string Catalog => nameof(NpcsDescriptor);

            public CatalogSchema Describe(ISchemaBuilder builder) => new(
                RootShape.ArrayUnderKey,
                [Section(NpcsKey, builder.Record(typeof(NpcDto)) with { IdField = IdField })],
                [],
                new FreeFilePlacement());
        }

        private sealed class LootDescriptor : ICatalogDescriptor
        {
            public string Catalog => nameof(LootDescriptor);

            public CatalogSchema Describe(ISchemaBuilder builder)
            {
                builder.Polymorphic(typeof(LootPositionDto), new VariantSet(
                [
                    Variant(IdField, builder.Record(typeof(LootPositionByIdDto))),
                    Variant(AugmentsField, builder.Record(typeof(LootPositionByAugmentsDto)))
                ]));

                RecordSchema table = builder.Record(typeof(LootTableDto));

                return new CatalogSchema(
                    RootShape.SectionsOfArrays,
                    [Section(GeneralKey, table), Section(IndividualKey, table)],
                    [],
                    new FreeFilePlacement());
            }
        }

        private sealed class KeyedDescriptor : ICatalogDescriptor
        {
            public string Catalog => nameof(KeyedDescriptor);

            public CatalogSchema Describe(ISchemaBuilder builder) => new(
                RootShape.Dictionary,
                [Section(string.Empty, builder.Record(typeof(AuthoredDto)))],
                [],
                new FreeFilePlacement());
        }

        private sealed class SingleDescriptor : ICatalogDescriptor
        {
            public string Catalog => nameof(SingleDescriptor);

            public CatalogSchema Describe(ISchemaBuilder builder) => new(
                RootShape.Single,
                [Section(string.Empty, builder.Record(typeof(AuthoredDto)))],
                [],
                new FreeFilePlacement());
        }

        private sealed class BareDescriptor : ICatalogDescriptor
        {
            public string Catalog => nameof(BareDescriptor);

            public CatalogSchema Describe(ISchemaBuilder builder) => new(
                RootShape.ArrayUnderKey,
                [Section(string.Empty, builder.Record(typeof(AuthoredDto)))],
                [],
                new FreeFilePlacement());
        }
    }
}

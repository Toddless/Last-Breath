namespace Tooling.Tests.DataEditor
{
    using Newtonsoft.Json.Linq;
    using Tooling.Catalogs;
    using Tooling.Json;
    using Tooling.Schema.Model;

    /// <summary>
    /// What the editor writes when the author asks for something that is not there yet: an element of a
    /// list, a key of a map, a key of a record, another shape for a record that has several. Every one of
    /// them puts a value into a file the game reads, so the question asked here is always the same — is
    /// what was written the least the author could have meant, and does one step back take all of it.
    /// </summary>
    [TestClass]
    public class RecordTemplatesTests
    {
        private const string IdField = "id";
        private const string PriceField = "price";
        private const string AugmentsField = "augments";
        private const string TierField = "tier";
        private const string RarityField = "rarity";
        private const string KindField = "kind";
        private const string FactorField = "factor";
        private const string AmountField = "amount";
        private const string NoteField = "note";

        private const string ScaleForm = "scale";
        private const string FlatForm = "flat";

        private const string Legendary = "Legendary";
        private const string Common = "Common";

        private const string PositionPointer = "/positions/0";

        private const string NamedPosition = """
            { "positions": [ { "id": "Item_Sword", "price": 10 } ] }
            """;

        private const string GroupPosition = """
            { "positions": [ { "augments": { "tier": 2, "rarity": "Common" }, "price": 10 } ] }
            """;

        private const string ScaledModifier = """
            { "modifiers": [ { "kind": "scale", "factor": 1.5 } ] }
            """;

        private const string BaseParameters = """
            { "baseParameters": {} }
            """;

        [TestMethod]
        public void Blank_AnswersEveryKindWithTheEmptyValueOfThatKind()
        {
            Assert.AreEqual(string.Empty, (string?)RecordTemplates.Blank(Field(IdField, FieldKind.String)));
            Assert.AreEqual(string.Empty, (string?)RecordTemplates.Blank(Field(IdField, FieldKind.Reference)));
            Assert.AreEqual(string.Empty, (string?)RecordTemplates.Blank(Field(IdField, FieldKind.LocalizedKey)));
            Assert.AreEqual(0, (int)RecordTemplates.Blank(Field(TierField, FieldKind.Integer)));
            Assert.AreEqual(0d, (double)RecordTemplates.Blank(Field(PriceField, FieldKind.Number)));
            Assert.IsFalse((bool)RecordTemplates.Blank(Field(NoteField, FieldKind.Boolean)));

            Assert.AreEqual(JTokenType.Array, RecordTemplates.Blank(
                new FieldSchema { JsonName = IdField, Kind = FieldKind.Array, Item = Field(IdField, FieldKind.String) }).Type);

            Assert.AreEqual(JTokenType.Object, RecordTemplates.Blank(
                new FieldSchema { JsonName = IdField, Kind = FieldKind.Dictionary, Item = Field(IdField, FieldKind.Number) }).Type);
        }

        /// <summary>An enum has no empty value of its own, so the first member stands for one. A member is
        /// a word the game recognises; the empty string is not, and a record laid down with one would be
        /// refused by the very reader the author is writing it for.</summary>
        [TestMethod]
        public void Blank_StandsAnEnumOnItsFirstMember()
        {
            Assert.AreEqual(Legendary, (string?)RecordTemplates.Blank(Rarity()));
            Assert.AreEqual(string.Empty, (string?)RecordTemplates.Blank(Field(RarityField, FieldKind.Enum)));
        }

        /// <summary>The default is what the game reads while the key is absent, so writing the key out
        /// with it is the one first value that changes nothing about the record.</summary>
        [TestMethod]
        public void Blank_TakesTheDefaultTheSchemaNames()
        {
            Assert.AreEqual(3, (int)RecordTemplates.Blank(Field(TierField, FieldKind.Integer) with { Default = 3 }));
            Assert.AreEqual(0.5d, (double)RecordTemplates.Blank(Field(PriceField, FieldKind.Number) with { Default = 0.5f }));
            Assert.IsTrue((bool)RecordTemplates.Blank(Field(NoteField, FieldKind.Boolean) with { Default = true }));
            Assert.AreEqual(Common, (string?)RecordTemplates.Blank(Rarity() with { Default = Common }));
        }

        /// <summary>A record is laid down with the keys it cannot be written without and no others: an
        /// optional key written for the author would be the tool deciding something about the record.</summary>
        [TestMethod]
        public void Blank_WritesTheRequiredKeysOfARecordAndNoOthers()
        {
            JObject blank = RecordTemplates.Blank(CatalogFixture.Record(
                IdField,
                Field(IdField, FieldKind.Reference) with { Required = true },
                Field(PriceField, FieldKind.Number) with { Required = true },
                Field(NoteField, FieldKind.String)));

            Assert.AreEqual(2, blank.Count);
            Assert.AreEqual(string.Empty, (string?)blank[IdField]);
            Assert.AreEqual(0d, (double)blank[PriceField]!);
            Assert.IsFalse(blank.ContainsKey(NoteField));
        }

        /// <summary>A record with shapes is written in one of them from the start: a position carrying
        /// neither of the two keys that name what drops is a position the game refuses to read.</summary>
        [TestMethod]
        public void Blank_WearsTheFirstShapeOfAPolymorphicRecord()
        {
            JObject blank = RecordTemplates.Blank(Positions());

            Assert.IsTrue(blank.ContainsKey(IdField));
            Assert.IsFalse(blank.ContainsKey(AugmentsField));
            Assert.AreEqual(0d, (double)blank[PriceField]!);
        }

        [TestMethod]
        public void Worn_AnswersByThePresenceOfAKeyAndByTheValueOfADiscriminator()
        {
            VariantSet positions = Forms();
            VariantSet modifiers = Kinds();

            Assert.AreEqual(IdField, RecordTemplates.Worn(positions, Position(NamedPosition))?.DiscriminatorValue);
            Assert.AreEqual(AugmentsField, RecordTemplates.Worn(positions, Position(GroupPosition))?.DiscriminatorValue);
            Assert.AreEqual(ScaleForm, RecordTemplates.Worn(modifiers, Modifier(ScaledModifier))?.DiscriminatorValue);

            Assert.IsNull(RecordTemplates.Worn(positions, new JObject()));
            Assert.IsNull(RecordTemplates.Worn(positions, new JArray()));
        }

        /// <summary>
        /// The keys of the shape being left go, the keys of the shape arriving come with their blanks,
        /// and what both shapes name keeps the value the author gave it.
        /// <para>One step of the history, not one per key: a record halfway between two shapes names both
        /// of two things it must name one of, and an undo that stopped there would leave the author
        /// holding a file the game will not read.</para>
        /// </summary>
        [TestMethod]
        public void SwitchVariant_TradesTheKeysOfOneShapeForTheOtherInOneStep()
        {
            JsonTreeDocument document = JsonTreeDocument.Parse(NamedPosition);
            JsonPointer at = JsonPointer.Parse(PositionPointer);
            VariantSet positions = Forms();

            Assert.IsTrue(RecordTemplates.SwitchVariant(document, at, positions, positions.Variants[1]));

            var group = (JObject)document.Resolve(at)!;

            Assert.IsFalse(group.ContainsKey(IdField));
            Assert.AreEqual(0, (int)group[AugmentsField]![TierField]!);
            Assert.AreEqual(Legendary, (string?)group[AugmentsField]![RarityField]);
            Assert.AreEqual(10d, (double)group[PriceField]!);

            Assert.AreEqual(1, document.History.Depth);
            document.History.Undo();

            var back = (JObject)document.Resolve(at)!;

            Assert.AreEqual("Item_Sword", (string?)back[IdField]);
            Assert.IsFalse(back.ContainsKey(AugmentsField));
            Assert.AreEqual(10d, (double)back[PriceField]!);
        }

        /// <summary>The other way round, so that neither shape is the one the trade happens to work for.</summary>
        [TestMethod]
        public void SwitchVariant_TakesAGroupPositionBackToOneNamingAThing()
        {
            JsonTreeDocument document = JsonTreeDocument.Parse(GroupPosition);
            JsonPointer at = JsonPointer.Parse(PositionPointer);
            VariantSet positions = Forms();

            Assert.IsTrue(RecordTemplates.SwitchVariant(document, at, positions, positions.Variants[0]));

            var named = (JObject)document.Resolve(at)!;

            Assert.IsFalse(named.ContainsKey(AugmentsField));
            Assert.AreEqual(string.Empty, (string?)named[IdField]);
            Assert.AreEqual(10d, (double)named[PriceField]!);
        }

        /// <summary>A shape named by the value of a field: the word is rewritten and the keys that only
        /// the old shape knew go with it.</summary>
        [TestMethod]
        public void SwitchVariant_RewritesTheWordThatNamesTheShape()
        {
            JsonTreeDocument document = JsonTreeDocument.Parse(ScaledModifier);
            JsonPointer at = JsonPointer.Parse("/modifiers/0");
            VariantSet kinds = Kinds();

            Assert.IsTrue(RecordTemplates.SwitchVariant(document, at, kinds, kinds.Variants[1]));

            var flat = (JObject)document.Resolve(at)!;

            Assert.AreEqual(FlatForm, (string?)flat[KindField]);
            Assert.IsFalse(flat.ContainsKey(FactorField));
            Assert.AreEqual(0, (int)flat[AmountField]!);
        }

        /// <summary>The shape it already wears, and an address holding no record: neither is a change, and
        /// a step filed for either would be a press of undo that takes back nothing.</summary>
        [TestMethod]
        public void SwitchVariant_FilesNoStepForAShapeAlreadyWornOrAnAddressWithNoRecord()
        {
            JsonTreeDocument document = JsonTreeDocument.Parse(NamedPosition);
            VariantSet positions = Forms();

            Assert.IsFalse(RecordTemplates.SwitchVariant(
                document, JsonPointer.Parse(PositionPointer), positions, positions.Variants[0]));

            Assert.IsFalse(RecordTemplates.SwitchVariant(
                document, JsonPointer.Parse("/positions/7"), positions, positions.Variants[1]));

            Assert.AreEqual(0, document.History.Depth);
        }

        /// <summary>A key of a map, added under a member of the enum that names its keys. What arrives
        /// under it is the blank of what the map holds, so the pair is complete the moment it is written
        /// and the author is left with one number to change instead of two things to invent.</summary>
        [TestMethod]
        public void AddedKey_WritesTheBlankOfTheMapsValueAndRefusesAKeyAlreadyThere()
        {
            JsonTreeDocument document = JsonTreeDocument.Parse(BaseParameters);
            JsonPointer at = JsonPointer.Parse("/baseParameters");
            FieldSchema value = Field(FieldSchema.Unnamed, FieldKind.Number);

            Assert.IsTrue(document.Insert(at, Legendary, RecordTemplates.Blank(value)));

            var map = (JObject)document.Resolve(at)!;

            Assert.AreEqual(0d, (double)map[Legendary]!);
            Assert.IsFalse(document.Insert(at, Legendary, RecordTemplates.Blank(value)));
            Assert.AreEqual(1, map.Count);
        }

        private static FieldSchema Field(string jsonName, FieldKind kind) => CatalogFixture.Field(jsonName, kind);

        private static FieldSchema Rarity() =>
            new() { JsonName = RarityField, Kind = FieldKind.Enum, EnumValues = [Legendary, Common], Required = true };

        private static JToken? Position(string json) => JsonTreeDocument.Parse(json).Resolve(JsonPointer.Parse(PositionPointer));

        private static JToken? Modifier(string json) => JsonTreeDocument.Parse(json).Resolve(JsonPointer.Parse("/modifiers/0"));

        /// <summary>A seat at a loot table: it names one thing, or a set of augments, and the presence of
        /// the key is what says which.</summary>
        private static VariantSet Forms() => new(
        [
            Shape(IdField, CatalogFixture.Record(
                null,
                Field(IdField, FieldKind.Reference) with { Required = true },
                Field(PriceField, FieldKind.Number) with { Required = true })),
            Shape(AugmentsField, CatalogFixture.Record(
                null,
                new FieldSchema
                {
                    JsonName = AugmentsField,
                    Kind = FieldKind.Object,
                    Record = CatalogFixture.Record(null, Field(TierField, FieldKind.Integer) with { Required = true }, Rarity())
                },
                Field(PriceField, FieldKind.Number) with { Required = true }))
        ]);

        /// <summary>A record with a field naming its shapes, which is the other way a schema tells them
        /// apart.</summary>
        private static VariantSet Kinds() => new(
        [
            Shape(ScaleForm, CatalogFixture.Record(null, Kind(), Field(FactorField, FieldKind.Number) with { Required = true })),
            Shape(FlatForm, CatalogFixture.Record(null, Kind(), Field(AmountField, FieldKind.Integer) with { Required = true }))
        ], KindField);

        private static RecordSchema Positions() => CatalogFixture.Record(null, Field(IdField, FieldKind.Reference)) with
        {
            Variants = Forms()
        };

        private static FieldSchema Kind() =>
            new() { JsonName = KindField, Kind = FieldKind.Enum, EnumValues = [ScaleForm, FlatForm], Required = true };

        private static VariantSchema Shape(string value, RecordSchema record) =>
            new() { DiscriminatorValue = value, Record = record };
    }
}

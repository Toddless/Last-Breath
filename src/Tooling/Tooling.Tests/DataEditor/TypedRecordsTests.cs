namespace Tooling.Tests.DataEditor
{
    using Newtonsoft.Json.Linq;
    using Tooling.Catalogs;
    using Tooling.Json;
    using Tooling.Schema.Model;

    /// <summary>
    /// A record whose shape a word it carries decides — a condition, an action — asked the four questions
    /// an editor asks of one: what type is written here, what a fresh one of a type looks like, what
    /// changing the type does to the keys already there, and how a lone entry becomes a list.
    /// <para>Every one of them writes into a file the game reads, so the question behind each is the same:
    /// is what was written the least the author could have meant, and does one step back take all of it.</para>
    /// </summary>
    [TestClass]
    public class TypedRecordsTests
    {
        private const string TypeKey = "type";

        private const string HasItem = "HasItem";
        private const string Fact = "Fact";
        private const string AllOf = "AllOf";
        private const string Unheard = "SacrificeGoat";

        private const string ItemField = "itemId";
        private const string AmountField = "amount";
        private const string KeyField = "key";
        private const string ValueField = "value";
        private const string ConditionsField = "conditions";
        private const string NoteField = "note";

        private const string EntryPointer = "/conditions/0";
        private const string ListPointer = "/conditions";
        private const string LonePointer = "/condition";

        private const string HasItemEntry = """
            { "conditions": [ { "type": "HasItem", "itemId": "Item_Sword", "amount": 2 } ] }
            """;

        private const string UnheardEntry = """
            { "conditions": [ { "type": "SacrificeGoat", "note": "at dawn" } ] }
            """;

        private const string LoneEntry = """
            { "condition": { "type": "Fact", "key": "Fact_Bridge", "value": true } }
            """;

        /// <summary>The word the file names the type with, whether or not the run knows it. An entry
        /// naming a type nobody registered still names one, and the author has to be shown the word.</summary>
        [TestMethod]
        public void Standing_ReadsTheWordTheFileWroteAndWornOnlyTheOneTheVocabularyHolds()
        {
            VocabularyBinding binding = Conditions();

            Assert.AreEqual(HasItem, TypedRecords.Standing(binding, Entry(HasItemEntry)));
            Assert.AreEqual(HasItem, TypedRecords.Worn(binding, Entry(HasItemEntry))?.TypeName);

            Assert.AreEqual(Unheard, TypedRecords.Standing(binding, Entry(UnheardEntry)));
            Assert.IsNull(TypedRecords.Worn(binding, Entry(UnheardEntry)), "a type the run does not know was answered for");

            Assert.AreEqual(string.Empty, TypedRecords.Standing(binding, new JObject()));
            Assert.AreEqual(string.Empty, TypedRecords.Standing(binding, new JArray()));
            Assert.IsNull(TypedRecords.Worn(binding, new JObject()));
        }

        /// <summary>A fresh entry carries the word naming it first and the keys its type cannot be written
        /// without — nothing else. An optional key written for the author would be the tool deciding
        /// something about the entry.</summary>
        [TestMethod]
        public void Blank_WritesTheTypeFirstAndOnlyTheKeysThatTypeRequires()
        {
            JObject blank = TypedRecords.Blank(Conditions(), HasItem);

            CollectionAssert.AreEqual(new[] { TypeKey, ItemField }, Keys(blank),
                "a fresh entry is written with other keys than the type requires, or in another order");
            Assert.AreEqual(HasItem, (string?)blank[TypeKey]);
            Assert.AreEqual(string.Empty, (string?)blank[ItemField]);
        }

        /// <summary>A type the run does not know is still written down as asked: the author names what the
        /// game will read, and a tool that refused a word it has no schema for would be refusing the only
        /// way a newer vocabulary can be authored at all.</summary>
        [TestMethod]
        public void Blank_WritesTheWordAloneForATypeTheRunDoesNotKnow()
        {
            JObject blank = TypedRecords.Blank(Conditions(), Unheard);

            CollectionAssert.AreEqual(new[] { TypeKey }, Keys(blank));
            Assert.AreEqual(Unheard, (string?)blank[TypeKey]);
        }

        /// <summary>
        /// The keys of the type being left go, the keys of the type arriving come with their blanks, and a
        /// key both types are written with keeps the value the author gave it — an amount typed under one
        /// condition is not something to type again under the next.
        /// <para>One step of the history, not one per key: an entry halfway between two types is one the
        /// game reads as neither.</para>
        /// </summary>
        [TestMethod]
        public void Switch_TradesTheKeysOfOneTypeForTheOtherAndKeepsWhatBothAreWrittenWith()
        {
            JsonTreeDocument document = JsonTreeDocument.Parse(HasItemEntry);
            JsonPointer at = JsonPointer.Parse(EntryPointer);

            Assert.IsTrue(TypedRecords.Switch(document, at, Conditions(), Fact));

            var written = (JObject)document.Resolve(at)!;

            Assert.AreEqual(Fact, (string?)written[TypeKey]);
            Assert.IsFalse(written.ContainsKey(ItemField), "a key only the outgoing type is written with stayed");
            Assert.AreEqual(string.Empty, (string?)written[KeyField], "a key the incoming type requires did not arrive");
            Assert.AreEqual(2, (int?)written[AmountField], "a key both types are written with lost the value it had");

            Assert.AreEqual(1, document.History.Depth);
            document.History.Undo();

            var back = (JObject)document.Resolve(at)!;

            Assert.AreEqual(HasItem, (string?)back[TypeKey]);
            Assert.AreEqual("Item_Sword", (string?)back[ItemField]);
            Assert.AreEqual(2, (int)back[AmountField]!);
        }

        /// <summary>The type it already names, and an address holding no entry: neither is a change, and a
        /// step filed for either would be a press of undo that takes back nothing.</summary>
        [TestMethod]
        public void Switch_FilesNoStepForATypeAlreadyNamedOrAnAddressWithNoEntry()
        {
            JsonTreeDocument document = JsonTreeDocument.Parse(HasItemEntry);

            Assert.IsFalse(TypedRecords.Switch(document, JsonPointer.Parse(EntryPointer), Conditions(), HasItem));
            Assert.IsFalse(TypedRecords.Switch(document, JsonPointer.Parse("/conditions/7"), Conditions(), Fact));
            Assert.IsFalse(TypedRecords.Switch(document, JsonPointer.Parse(ListPointer), Conditions(), Fact));

            Assert.AreEqual(0, document.History.Depth);
        }

        /// <summary>An entry naming a type the run does not know is not read as an empty one: nothing under
        /// it is shed, because nothing here can say which of its keys the outgoing type owned.</summary>
        [TestMethod]
        public void Switch_KeepsEveryKeyOfAnEntryWhoseTypeTheRunDoesNotKnow()
        {
            JsonTreeDocument document = JsonTreeDocument.Parse(UnheardEntry);
            JsonPointer at = JsonPointer.Parse(EntryPointer);

            Assert.IsTrue(TypedRecords.Switch(document, at, Conditions(), Fact));

            var written = (JObject)document.Resolve(at)!;

            Assert.AreEqual(Fact, (string?)written[TypeKey]);
            Assert.AreEqual("at dawn", (string?)written[NoteField], "a key no known type claimed was dropped");
            Assert.AreEqual(string.Empty, (string?)written[KeyField]);
        }

        /// <summary>The word naming the type keeps the place it stands in, so a change of type is a diff of
        /// one line and not of a whole entry rewritten in another order.</summary>
        [TestMethod]
        public void Switch_LeavesTheTypeWhereTheFileWroteIt()
        {
            JsonTreeDocument document = JsonTreeDocument.Parse(HasItemEntry);
            JsonPointer at = JsonPointer.Parse(EntryPointer);

            Assert.IsTrue(TypedRecords.Switch(document, at, Conditions(), Fact));
            Assert.AreEqual(TypeKey, Keys((JObject)document.Resolve(at)!)[0]);
        }

        /// <summary>
        /// A lone entry written under a key the game reads as a list becomes a list of one, in a single
        /// step of the history: it is one gesture, and an undo that left the file as a list holding what
        /// the author never asked to be listed would be the tool keeping half a change.
        /// </summary>
        [TestMethod]
        public void AsList_WrapsALoneEntryInOneStepThatUndoTakesBackWhole()
        {
            JsonTreeDocument document = JsonTreeDocument.Parse(LoneEntry);
            JsonPointer at = JsonPointer.Parse(LonePointer);

            Assert.IsTrue(TypedRecords.AsList(document, at));

            var list = (JArray)document.Resolve(at)!;

            Assert.AreEqual(1, list.Count);
            Assert.AreEqual(Fact, (string?)list[0][TypeKey]);
            Assert.AreEqual("Fact_Bridge", (string?)list[0][KeyField]);

            Assert.AreEqual(1, document.History.Depth, "wrapping a lone entry is not one step of the history");

            document.History.Undo();

            Assert.AreEqual(JTokenType.Object, document.Resolve(at)!.Type, "one step back did not take the whole gesture");
            Assert.AreEqual(Fact, (string?)document.Resolve(at)![TypeKey]);
        }

        /// <summary>A list already, and an address holding nothing: there is no lone entry to move, and a
        /// step filed for either would be a press of undo that takes back nothing.</summary>
        [TestMethod]
        public void AsList_FilesNoStepWhereThereIsNoLoneEntry()
        {
            JsonTreeDocument document = JsonTreeDocument.Parse(HasItemEntry);

            Assert.IsFalse(TypedRecords.AsList(document, JsonPointer.Parse(ListPointer)));
            Assert.IsFalse(TypedRecords.AsList(document, JsonPointer.Parse(LonePointer)));

            Assert.AreEqual(0, document.History.Depth);
        }

        /// <summary>A type written with entries of the vocabulary under it — the way a composite condition
        /// is — is laid down with the empty list it requires, so the block the author adds to is there the
        /// moment the entry is.</summary>
        [TestMethod]
        public void Blank_LaysDownTheEmptyListACompositeTypeIsWrittenWith()
        {
            JObject blank = TypedRecords.Blank(Conditions(), AllOf);

            CollectionAssert.AreEqual(new[] { TypeKey, ConditionsField }, Keys(blank));
            Assert.AreEqual(JTokenType.Array, blank[ConditionsField]!.Type);
            Assert.AreEqual(0, ((JArray)blank[ConditionsField]!).Count);
        }

        /// <summary>
        /// A key the vocabulary is read as a LIST under opens as an empty list. The schema can only call
        /// such a key free json, and the blank of free json is an empty object: written that way, the key
        /// would hold something the game's list parser walks straight past, while the editor drew a lone
        /// entry the author would go on filling in.
        /// <para>A key read as one entry keeps the blank of its own kind, and so does a key no vocabulary
        /// answers for: this rule is about the shape the game reads and about nothing else.</para>
        /// </summary>
        [TestMethod]
        public void Blank_OpensAKeyReadAsAListAsAnEmptyListAndNotAsAnEmptyObject()
        {
            FieldSchema free = new() { JsonName = ConditionsField, Kind = FieldKind.Any };

            JToken list = TypedRecords.Blank(Conditions(), free);

            Assert.AreEqual(JTokenType.Array, list.Type, "a key the game reads as a list was opened as something else");
            Assert.AreEqual(0, ((JArray)list).Count, "a key opened with an entry nobody asked for");

            Assert.AreEqual(JTokenType.Object, TypedRecords.Blank(Conditions() with { List = false }, free).Type,
                "a key read as one entry was opened as a list");
            Assert.AreEqual(JTokenType.Object, TypedRecords.Blank(binding: null, free).Type,
                "a key no vocabulary answers for was opened as something other than its own blank");
        }

        /// <summary>A key no vocabulary answers for is opened exactly as its own kind is, whatever that
        /// kind happens to be: nothing here is allowed to reach past the vocabulary.</summary>
        [TestMethod]
        public void Blank_LeavesAKeyOutsideTheVocabularyToItsOwnKind()
        {
            Assert.AreEqual(JTokenType.Integer, TypedRecords.Blank(binding: null, CatalogFixture.Field(AmountField, FieldKind.Integer)).Type);
            Assert.AreEqual(JTokenType.String, TypedRecords.Blank(binding: null, CatalogFixture.Field(KeyField, FieldKind.String)).Type);
        }

        private static JToken? Entry(string json) => JsonTreeDocument.Parse(json).Resolve(JsonPointer.Parse(EntryPointer));

        private static string[] Keys(JObject holder) => [.. holder.Properties().Select(property => property.Name)];

        /// <summary>A vocabulary of three types standing for the narrative's own: two plain ones sharing a
        /// key, and one written with entries of the vocabulary under it.</summary>
        private static VocabularyBinding Conditions() => new(
            [
                CatalogFixture.Record(null,
                    CatalogFixture.Required(ItemField, FieldKind.Reference),
                    CatalogFixture.Field(AmountField, FieldKind.Integer)) with { TypeName = HasItem },
                CatalogFixture.Record(null,
                    CatalogFixture.Required(KeyField, FieldKind.String),
                    CatalogFixture.Field(ValueField, FieldKind.Boolean),
                    CatalogFixture.Field(AmountField, FieldKind.Integer)) with { TypeName = Fact },
                CatalogFixture.Record(null,
                    CatalogFixture.Required(ConditionsField, FieldKind.Array)) with { TypeName = AllOf }
            ],
            TypeKey,
            List: true);
    }
}

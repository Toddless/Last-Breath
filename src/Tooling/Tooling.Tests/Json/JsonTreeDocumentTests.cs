namespace Tooling.Tests.Json
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Newtonsoft.Json.Linq;
    using Tooling.Editing.History;
    using Tooling.Json;

    /// <summary>
    /// The document under an authoring tool. Every test here asks the same two questions of one operation:
    /// did it change exactly what it named, and does a step back put the file byte for byte where it was.
    /// Both are invisible on screen — an undo that leaves a key in the wrong place, or a value the author
    /// never had, is found in the diff of a file that was already committed.
    /// </summary>
    [TestClass]
    public class JsonTreeDocumentTests
    {
        private const string Json = """
            {
                "id": "Npc_Ronald",
                "level": 3,
                "tags": [ "human", "trader" ],
                "home": { "x": 12, "y": -4.5 }
            }
            """;

        private const string Id = "id";
        private const string Level = "level";
        private const string Tags = "tags";
        private const string Home = "home";
        private const string Nothing = "nothing";
        private const string Trade = "trade";
        private const string Renamed = "rank";

        private const string FirstName = "Ronald";
        private const string SecondName = "Ronald the Smith";
        private const string ThirdName = "Ronald Ironhand";
        private const string AddedTag = "smith";

        private string _file = string.Empty;

        [TestInitialize]
        public void CreateTempPath() => _file = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".json");

        [TestCleanup]
        public void DeleteTempFile()
        {
            if (File.Exists(_file)) File.Delete(_file);
        }

        [TestMethod]
        public void Parse_RefusesARootThatIsNotAContainer()
        {
            Assert.ThrowsException<FormatException>(() => JsonTreeDocument.Parse("42"));
        }

        /// <summary>What the document cannot carry through a save it refuses to open. A key written twice
        /// would come back as one and a second document would go missing on the first save — a file the tool
        /// cannot keep whole is a file the tool has no business rewriting.</summary>
        [TestMethod]
        public void Parse_RefusesAFileItCouldNotWriteBackWhole()
        {
            Assert.ThrowsException<FormatException>(() => JsonTreeDocument.Parse("""{"a":1,"a":2}"""), "a key written twice");
            Assert.ThrowsException<FormatException>(() => JsonTreeDocument.Parse("""{"a":1} {"b":2}"""), "something standing after the document");
        }

        /// <summary>An empty or broken file is refused the same way a badly shaped one is: a caller that had
        /// to tell a reader error from a shape error apart would catch both anyway.</summary>
        [TestMethod]
        public void Parse_RefusesAnEmptyOrBrokenFile()
        {
            Assert.ThrowsException<FormatException>(() => JsonTreeDocument.Parse(string.Empty));
            Assert.ThrowsException<FormatException>(() => JsonTreeDocument.Parse("   "));
            Assert.ThrowsException<FormatException>(() => JsonTreeDocument.Parse("""{"a":"""));
        }

        /// <summary>A value nobody edited has to come back out as it went in: a date-shaped string that became
        /// a date would be written in another format, and a fractional number read as a decimal in another
        /// number of digits. Both rewrite lines the author never touched.</summary>
        [TestMethod]
        public void Parse_LeavesDateShapedStringsAndNumbersAlone()
        {
            const string source = """{"when":"2026-09-02T10:00:00Z","rate":1.5}""";

            JsonTreeDocument document = JsonTreeDocument.Parse(source);

            Assert.AreEqual(JTokenType.String, document.Resolve(JsonPointer.Root.Append("when"))?.Type);
            Assert.AreEqual(JTokenType.Float, document.Resolve(JsonPointer.Root.Append("rate"))?.Type);
            StringAssert.Contains(Canon(document), "2026-09-02T10:00:00Z");
        }

        [TestMethod]
        public void SetValue_WritesTheNode_AndUndoPutsTheFileBack()
        {
            JsonTreeDocument document = JsonTreeDocument.Parse(Json);
            string before = Canon(document);

            Assert.IsTrue(document.SetValue(At(Id), new JValue(SecondName)));
            Assert.AreEqual(SecondName, document.Resolve(At(Id))?.Value<string>());
            Assert.AreNotEqual(before, Canon(document));

            document.History.Undo();

            Assert.AreEqual(before, Canon(document));
        }

        [TestMethod]
        public void SetValue_OnAPathTheTreeDoesNotHave_ChangesNothing()
        {
            JsonTreeDocument document = JsonTreeDocument.Parse(Json);
            string before = Canon(document);

            Assert.IsFalse(document.SetValue(At(Nothing), new JValue(1)));
            Assert.AreEqual(before, Canon(document));
            Assert.AreEqual(0, document.History.Depth);
        }

        /// <summary>Typing a name is one thing the author did, so it is one thing to take back — and the step
        /// lands on the name the file had, not on the letter before last.</summary>
        [TestMethod]
        public void SetValue_MergesARunOnOnePointer_IntoOneStep()
        {
            JsonTreeDocument document = JsonTreeDocument.Parse(Json);
            string before = Canon(document);

            document.SetValue(At(Id), new JValue(SecondName));
            document.SetValue(At(Id), new JValue(ThirdName));

            Assert.AreEqual(1, document.History.Depth);

            document.History.Undo();

            Assert.AreEqual(before, Canon(document));
            Assert.IsFalse(document.History.CanUndo);
        }

        /// <summary>The same address after a structural edit is another node, and taking it back is another
        /// step. Merging on the text of the pointer alone would put two edits of two different nodes under
        /// one press of undo, and the state it restored would be neither of them.</summary>
        [TestMethod]
        public void SetValue_AfterAStructuralEdit_IsAnotherStep_AtTheSameAddress()
        {
            JsonTreeDocument document = JsonTreeDocument.Parse(Json);
            JsonPointer first = At(Tags).Append(0);
            string before = Canon(document);

            document.SetValue(first, new JValue(AddedTag));
            document.Remove(first);
            document.SetValue(first, new JValue(AddedTag));

            Assert.AreEqual(3, document.History.Depth);

            document.History.Undo();
            document.History.Undo();
            document.History.Undo();

            Assert.AreEqual(before, Canon(document));
        }

        /// <summary>Writing what already stands is not an edit. A step filed for it would report the document
        /// as unsaved with nothing to save, and the undo the author then pressed would do nothing visible.</summary>
        [TestMethod]
        public void SetValue_OfTheValueThatIsAlreadyThere_IsNotAStep()
        {
            JsonTreeDocument document = JsonTreeDocument.Parse(Json);
            document.Save(_file, FileKeyOrder.Instance);

            Assert.IsTrue(document.SetValue(At(Level), new JValue(3)));
            Assert.AreEqual(0, document.History.Depth);
            Assert.IsTrue(document.IsClean);
        }

        /// <summary>The kind of a number is part of what the file says: 3 and 3.0 are the same value and two
        /// different lines, so changing one into the other is an edit like any other.</summary>
        [TestMethod]
        public void SetValue_ThatOnlyChangesTheKindOfANumber_IsAStep()
        {
            JsonTreeDocument document = JsonTreeDocument.Parse(Json);

            Assert.IsTrue(document.SetValue(At(Level), new JValue(3.0)));
            Assert.AreEqual(1, document.History.Depth);
            StringAssert.Contains(Canon(document), "\"level\": 3.0");
        }

        [TestMethod]
        public void SetValue_OnAnotherPointer_IsAnotherStep()
        {
            JsonTreeDocument document = JsonTreeDocument.Parse(Json);

            document.SetValue(At(Id), new JValue(SecondName));
            document.SetValue(At(Level), new JValue(4));

            Assert.AreEqual(2, document.History.Depth);

            document.History.Undo();

            Assert.AreEqual(3, document.Resolve(At(Level))?.Value<int>());
            Assert.AreEqual(SecondName, document.Resolve(At(Id))?.Value<string>());
        }

        [TestMethod]
        public void Insert_AddsAKey_AndUndoTakesItBackOut()
        {
            JsonTreeDocument document = JsonTreeDocument.Parse(Json);
            string before = Canon(document);

            Assert.IsTrue(document.Insert(JsonPointer.Root, Trade, new JValue(true)));
            Assert.IsTrue(document.Resolve(At(Trade))!.Value<bool>());

            document.History.Undo();

            Assert.AreEqual(before, Canon(document));
        }

        /// <summary>A key that is already there is replaced by <see cref="JsonTreeDocument.SetValue"/>, which
        /// remembers the value it replaced. An insert that overwrote would drop it with nothing to restore.</summary>
        [TestMethod]
        public void Insert_AKeyThatIsAlreadyThere_IsRefused()
        {
            JsonTreeDocument document = JsonTreeDocument.Parse(Json);
            string before = Canon(document);

            Assert.IsFalse(document.Insert(JsonPointer.Root, Id, new JValue(SecondName)));
            Assert.IsFalse(document.Insert(At(Nothing), Trade, new JValue(1)), "there is no object at that pointer");
            Assert.AreEqual(before, Canon(document));
            Assert.AreEqual(0, document.History.Depth);
        }

        [TestMethod]
        public void Insert_PutsAnItemIntoTheArray_AndUndoTakesItOut()
        {
            JsonTreeDocument document = JsonTreeDocument.Parse(Json);
            string before = Canon(document);

            Assert.IsTrue(document.Insert(At(Tags), 1, new JValue(AddedTag)));
            Assert.AreEqual(AddedTag, document.Resolve(At(Tags).Append(1))?.Value<string>());
            Assert.AreEqual(3, ((JArray)document.Resolve(At(Tags))!).Count);

            document.History.Undo();

            Assert.AreEqual(before, Canon(document));
        }

        [TestMethod]
        public void Insert_AnIndexOutsideTheArray_IsRefused()
        {
            JsonTreeDocument document = JsonTreeDocument.Parse(Json);

            Assert.IsFalse(document.Insert(At(Tags), 3, new JValue(AddedTag)));
            Assert.IsFalse(document.Insert(At(Tags), -1, new JValue(AddedTag)));
            Assert.IsTrue(document.Insert(At(Tags), 2, new JValue(AddedTag)), "the end of the array is a place to insert");
        }

        /// <summary>The document owns its nodes. Sharing one with the caller means an edit made through the
        /// caller's own reference never reaches the history, and an undo cannot take back what it never saw.</summary>
        [TestMethod]
        public void Insert_AndSetValue_CloneWhatTheyAreGiven()
        {
            JsonTreeDocument document = JsonTreeDocument.Parse(Json);

            var inserted = new JObject { [Level] = 1 };
            document.Insert(JsonPointer.Root, Trade, inserted);
            inserted[Level] = 99;

            var written = new JObject { [Level] = 2 };
            document.SetValue(At(Home), written);
            written[Level] = 99;

            Assert.AreEqual(1, document.Resolve(At(Trade).Append(Level))?.Value<int>());
            Assert.AreEqual(2, document.Resolve(At(Home).Append(Level))?.Value<int>());
        }

        /// <summary>A key that came back at the end of its object is a diff of its own, so the undo restores
        /// the place as well as the value.</summary>
        [TestMethod]
        public void Remove_TakesTheKeyOut_AndUndoRestoresItsPlace()
        {
            JsonTreeDocument document = JsonTreeDocument.Parse(Json);
            string before = Canon(document);

            Assert.IsTrue(document.Remove(At(Level)));
            Assert.IsNull(document.Resolve(At(Level)));

            document.History.Undo();

            Assert.AreEqual(before, Canon(document));
        }

        [TestMethod]
        public void Remove_TakesTheItemOut_AndUndoPutsItBackWhereItStood()
        {
            JsonTreeDocument document = JsonTreeDocument.Parse(Json);
            string before = Canon(document);

            Assert.IsTrue(document.Remove(At(Tags).Append(0)));
            Assert.AreEqual(1, ((JArray)document.Resolve(At(Tags))!).Count);

            document.History.Undo();

            Assert.AreEqual(before, Canon(document));
        }

        [TestMethod]
        public void Remove_TheRootOrAMissingPath_IsRefused()
        {
            JsonTreeDocument document = JsonTreeDocument.Parse(Json);

            Assert.IsFalse(document.Remove(JsonPointer.Root));
            Assert.IsFalse(document.Remove(At(Nothing)));
            Assert.AreEqual(0, document.History.Depth);
        }

        [TestMethod]
        public void Move_ReordersTheArray_AndUndoPutsTheOrderBack()
        {
            JsonTreeDocument document = JsonTreeDocument.Parse(Json);
            string before = Canon(document);

            Assert.IsTrue(document.Move(At(Tags).Append(0), 1));
            Assert.AreEqual("trader", document.Resolve(At(Tags).Append(0))?.Value<string>());

            document.History.Undo();

            Assert.AreEqual(before, Canon(document));
        }

        [TestMethod]
        public void Move_ThatChangesNothing_IsRefused()
        {
            JsonTreeDocument document = JsonTreeDocument.Parse(Json);

            Assert.IsFalse(document.Move(At(Tags).Append(0), 0), "the index it already has");
            Assert.IsFalse(document.Move(At(Tags).Append(0), 2), "past the end of the array");
            Assert.IsFalse(document.Move(At(Id), 0), "the node is not in an array");
            Assert.AreEqual(0, document.History.Depth);
        }

        [TestMethod]
        public void RenameKey_KeepsThePlaceAndTheValue_AndUndoNamesItBack()
        {
            JsonTreeDocument document = JsonTreeDocument.Parse(Json);
            string before = Canon(document);

            Assert.IsTrue(document.RenameKey(At(Level), Renamed));
            Assert.IsNull(document.Resolve(At(Level)));
            Assert.AreEqual(3, document.Resolve(At(Renamed))?.Value<int>());
            Assert.AreEqual(1, IndexOfKey(document, Renamed), "the key stays where it stood");

            document.History.Undo();

            Assert.AreEqual(before, Canon(document));
        }

        [TestMethod]
        public void RenameKey_ToAKeyThatIsTaken_IsRefused()
        {
            JsonTreeDocument document = JsonTreeDocument.Parse(Json);
            string before = Canon(document);

            Assert.IsFalse(document.RenameKey(At(Level), Id));
            Assert.IsFalse(document.RenameKey(At(Level), Level), "the name it already has");
            Assert.IsFalse(document.RenameKey(At(Level), string.Empty));
            Assert.AreEqual(before, Canon(document));
            Assert.AreEqual(0, document.History.Depth);
        }

        /// <summary>A step forward puts back exactly what the step back took away. Undo and redo run different
        /// halves of a command, and a redo that appended where the edit had inserted, or moved to the index it
        /// came from, would be found only after the author walked the history and saved.</summary>
        [TestMethod]
        public void Redo_PutsBackWhatTheMoveDid() => AssertRedoRepeats(document => document.Move(At(Tags).Append(0), 1));

        [TestMethod]
        public void Redo_PutsBackWhatTheRemoveDid() => AssertRedoRepeats(document => document.Remove(At(Level)));

        [TestMethod]
        public void Redo_PutsBackWhatTheInsertDid() => AssertRedoRepeats(document => document.Insert(At(Tags), 0, new JValue(AddedTag)));

        [TestMethod]
        public void Redo_PutsBackWhatTheRenameDid() => AssertRedoRepeats(document => document.RenameKey(At(Level), Renamed));

        /// <summary>What changed is said in the one language the document speaks: a value edit names the node,
        /// a structural edit names the container whose contents shifted under it.</summary>
        [TestMethod]
        public void Changed_CarriesThePointer_OnEditAndOnUndo()
        {
            JsonTreeDocument document = JsonTreeDocument.Parse(Json);
            List<JsonPointer> heard = [];
            document.Changed += pointer => heard.Add(pointer);

            document.SetValue(At(Id), new JValue(SecondName));
            document.History.Seal();
            document.Insert(At(Tags), 0, new JValue(AddedTag));

            CollectionAssert.AreEqual(new[] { At(Id), At(Tags) }, heard);

            document.History.Undo();
            document.History.Undo();

            CollectionAssert.AreEqual(new[] { At(Id), At(Tags), At(Tags), At(Id) }, heard);
        }

        [TestMethod]
        public void Save_WritesTheFile_AndTheHistoryCallsTheDocumentClean()
        {
            JsonTreeDocument document = JsonTreeDocument.Parse(Json);
            document.SetValue(At(Id), new JValue(SecondName));

            Assert.IsFalse(document.IsClean);

            document.Save(_file, FileKeyOrder.Instance);

            Assert.IsTrue(document.IsClean);
            Assert.AreEqual(Canon(document), File.ReadAllText(_file));

            document.History.Undo();

            Assert.IsFalse(document.IsClean, "the tree on screen is no longer the tree on disk");
        }

        /// <summary>The whole document can be replaced — a reload, a paste over everything — and that is a
        /// step like any other.</summary>
        [TestMethod]
        public void SetValue_AtTheRoot_ReplacesTheDocument()
        {
            JsonTreeDocument document = JsonTreeDocument.Parse(Json);
            string before = Canon(document);

            Assert.IsFalse(document.SetValue(JsonPointer.Root, new JValue(1)), "the root is a container");
            Assert.IsTrue(document.SetValue(JsonPointer.Root, new JObject { [Id] = FirstName }));
            Assert.AreEqual(FirstName, document.Resolve(At(Id))?.Value<string>());

            document.History.Undo();

            Assert.AreEqual(before, Canon(document));
        }

        /// <summary>
        /// Putting a value where the tree holds the key and where it does not: the key is written in the
        /// second case and replaced in the first, and either way it is one step of the history. This is
        /// what the first edit of a field the file never wrote has to mean — an author reading a default
        /// off the screen cannot tell whether the key is there, and must not have to.
        /// </summary>
        [TestMethod]
        public void Put_WritesTheKeyWhereTheTreeHasNoneAndReplacesItWhereItHas()
        {
            JsonTreeDocument document = JsonTreeDocument.Parse(Json);

            Assert.IsTrue(document.Put(At(Level), new JValue(9)));
            Assert.AreEqual(9, document.Resolve(At(Level))?.Value<int>());

            Assert.IsTrue(document.Put(At(Trade), new JValue(FirstName)));
            Assert.AreEqual(FirstName, document.Resolve(At(Trade))?.Value<string>());

            Assert.AreEqual(2, document.History.Depth, "putting a value is more than one step of the history");

            document.History.Undo();

            Assert.IsNull(document.Resolve(At(Trade)), "one step back left the key the put laid down");

            // The root cannot become a value and has no key to be written under: a put there is refused
            // the way a set is, rather than replacing the document with a number.
            Assert.IsFalse(document.Put(JsonPointer.Root, new JValue(1)));
        }

        /// <summary>
        /// Two files of one tool record onto the stack the tool handed them, so one press of undo takes back
        /// the author's last edit whichever file it was made in — and each file still answers for itself
        /// whether it is the file on disk, because a save writes them one at a time.
        /// </summary>
        [TestMethod]
        public void Parse_OntoAnOutsideHistory_FilesEveryDocumentsEditsOnIt()
        {
            EditHistory history = new();
            JsonTreeDocument one = JsonTreeDocument.Parse(Json, history);
            JsonTreeDocument another = JsonTreeDocument.Parse(Json, history);

            Assert.IsTrue(one.SetValue(At(Level), new JValue(9)));
            Assert.IsTrue(another.SetValue(At(Level), new JValue(7)));

            Assert.AreEqual(2, history.Depth, "a run of keystrokes does not reach across two documents");
            Assert.IsFalse(one.IsClean);
            Assert.IsFalse(another.IsClean);

            history.Undo();

            Assert.AreEqual(3, another.Resolve(At(Level))?.Value<int>(), "the newest edit was the other file's");
            Assert.AreEqual(9, one.Resolve(At(Level))?.Value<int>());
            Assert.IsTrue(another.IsClean);
            Assert.IsFalse(one.IsClean);
        }

        /// <summary>A file the run lays down is written into before anything takes it in. It joins the
        /// tool's stack with that first step, or the one gesture nobody could take back would be the one
        /// that created a file.</summary>
        [TestMethod]
        public void Follow_BringsTheStepsAlreadyTakenOntoTheNewHistory()
        {
            EditHistory history = new();
            JsonTreeDocument document = JsonTreeDocument.Parse(Json);

            Assert.IsTrue(document.SetValue(At(Level), new JValue(9)));

            document.Follow(history);

            Assert.AreEqual(1, history.Depth);
            Assert.IsFalse(document.IsClean);

            history.Undo();

            Assert.AreEqual(3, document.Resolve(At(Level))?.Value<int>());
        }

        /// <summary>One edit, walked back and forward: the file has to be the same on both ends of the walk.</summary>
        private static void AssertRedoRepeats(Func<JsonTreeDocument, bool> edit)
        {
            JsonTreeDocument document = JsonTreeDocument.Parse(Json);
            string before = Canon(document);

            Assert.IsTrue(edit(document));

            string after = Canon(document);

            document.History.Undo();

            Assert.AreEqual(before, Canon(document));

            document.History.Redo();

            Assert.AreEqual(after, Canon(document));
        }

        private static JsonPointer At(string key) => JsonPointer.Root.Append(key);

        private static string Canon(JsonTreeDocument document) => document.Write(FileKeyOrder.Instance);

        private static int IndexOfKey(JsonTreeDocument document, string key)
        {
            var root = (JObject)document.Root;
            int at = 0;

            foreach (JProperty property in root.Properties())
            {
                if (property.Name == key) return at;

                at++;
            }

            return -1;
        }
    }
}

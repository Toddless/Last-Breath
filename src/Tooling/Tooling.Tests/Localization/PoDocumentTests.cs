namespace Tooling.Tests.Localization
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Tooling.Editing.History;
    using Tooling.Localization;

    /// <summary>
    /// The catalog under an authoring tool. The tool writes these files next to a hand editing them, so every
    /// test here asks the same two questions of one operation: did it change exactly the line it named, and
    /// does a step back leave the file byte for byte where it was. Both are invisible on screen — a rewritten
    /// entry nobody touched is found in the diff of a commit that has already gone in.
    /// </summary>
    [TestClass]
    public class PoDocumentTests
    {
        /// <summary>The first thirty lines of the game's own en.po: the header laid out across several lines,
        /// a section banner, and entries both translated and empty.</summary>
        private const string Fragment = """
            msgid ""
            msgstr ""
            "Content-Type: text/plain; charset=UTF-8\n"
            "Plural-Forms: nplurals=2; plural=(n != 1);\n"
            "Language: en\n"

            # -----------------------
            # Equip items
            # -----------------------

            msgid "Weapon_Bloodthirsty"
            msgstr "Bloodthirsty"

            msgid "Weapon_Bloodthirsty_Description"
            msgstr ""

            msgid "Weapon_Silent_Fury"
            msgstr "Silent Fury"

            msgid "Weapon_Silent_Fury_Description"
            msgstr ""

            msgid "Amulet_Creators_Nature"
            msgstr "Creator`s Nature"

            msgid "Amulet_Creators_Nature_Description"
            msgstr ""

            msgid "Amulet_Diamond"
            msgstr "Diamond Amulet"
            """;

        private const string Plurals = """
            msgid ""
            msgstr ""
            "Language: en\n"

            # ------------------------------
            # Counted words
            # ------------------------------

            msgid "turn"
            msgid_plural "turns"
            msgstr[0] "turn"
            msgstr[1] "turns"

            msgid "point"
            msgid_plural "points"
            msgstr[0] "point"
            msgstr[1] "points"
            """;

        private const string Multiline = """
            msgid ""
            msgstr ""
            "Language: en\n"

            msgid "Long_Text"
            msgstr ""
            "first line\n"
            "second line"

            msgid "After"
            msgstr "after"
            """;

        private const string Escaped = """
            msgid ""
            msgstr ""
            "Language: en\n"

            msgid "Quoted"
            msgstr "He said \"no\" \\ and left.\nНа русском\tтоже."
            """;

        /// <summary>The shape of en.po around line 894: a section banner written straight above the first
        /// entry of the section, with no blank line between them.</summary>
        private const string Banner = """
            msgid ""
            msgstr ""
            "Language: en\n"

            msgid "Crafting_Resource_Essence_Barrier"
            msgstr "Essence of Barrier"

            # -----------------------
            # Essences
            # -----------------------
            msgid "Crafting_Resource_Armor_Essence"
            msgstr "Essence of Defence"

            msgid "Crafting_Resource_Crit_Essence"
            msgstr "Essence of Death"
            """;

        /// <summary>The shape of en.po around line 4655: two blank lines breaking one section off the next.</summary>
        private const string Sections = """
            msgid ""
            msgstr ""
            "Language: en\n"

            msgid "Dlg_Ronald_TrialPraiseLast_1"
            msgstr "Done, and done well."


            msgid "Augment_Weapon_Scale"
            msgstr "Weapon Scaling"
            """;

        private const string Twice = """
            msgid ""
            msgstr ""
            "Language: en\n"

            msgid "Repeated"
            msgstr "first"

            msgid "Repeated"
            msgstr "second"
            """;

        private const string Bloodthirsty = "Weapon_Bloodthirsty";
        private const string SilentFury = "Weapon_Silent_Fury";
        private const string Diamond = "Amulet_Diamond";
        private const string Quoted = "Quoted";
        private const string LongText = "Long_Text";
        private const string Turn = "turn";
        private const string ArmorEssence = "Crafting_Resource_Armor_Essence";
        private const string LastPraise = "Dlg_Ronald_TrialPraiseLast_1";
        private const string Scale = "Augment_Weapon_Scale";
        private const string Repeated = "Repeated";
        private const string Missing = "Weapon_Nothing";
        private const string Added = "Weapon_Rusty";
        private const string Renamed = "Weapon_Quiet_Fury";

        private string _file = string.Empty;

        [TestInitialize]
        public void CreateTempPath() => _file = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".po");

        [TestCleanup]
        public void DeleteTempFile()
        {
            if (File.Exists(_file)) File.Delete(_file);
        }

        /// <summary>The whole promise of the document in one assertion: a file read and written again is the
        /// same file. Everything else the tool does is allowed to change only what the author asked for.</summary>
        [TestMethod]
        public void Parse_KeepsARealFragmentByteForByte()
        {
            string source = Source(Fragment);
            PoDocument document = PoDocument.Parse(source);

            Assert.AreEqual(source, document.Write());
            Assert.AreEqual(0, document.Warnings.Count);
        }

        [TestMethod]
        public void Parse_ReadsTheHeaderTheBannersAndTheEntries()
        {
            PoDocument document = PoDocument.Parse(Source(Fragment));

            Assert.IsNotNull(document.Header);
            Assert.IsTrue(document.Header!.IsHeader);
            StringAssert.Contains(document.Header.MsgStr, "Language: en\n");
            Assert.AreEqual(8, document.Entries.Count(), "seven keys and the header");
            Assert.AreEqual("Bloodthirsty", document.TryGet(Bloodthirsty)?.MsgStr);
            Assert.IsTrue(document.TryGet(Bloodthirsty)!.IsTranslated);
            Assert.IsFalse(document.TryGet(Bloodthirsty + "_Description")!.IsTranslated);
            Assert.IsTrue(
                document.Blocks.OfType<PoTextBlock>().Any(block => block.Lines.Contains("# Equip items")),
                "the section banner is a block of its own, belonging to no entry");
        }

        /// <summary>A banner sits above the entry after it, but it is the section's and not that entry's: an
        /// entry that swallowed it would carry it away when the author deleted the key.</summary>
        [TestMethod]
        public void Parse_GivesAnEntryOnlyTheCommentsWrittenAgainstIt()
        {
            const string source = """
                #. said by the smith
                msgid "Greeting"
                msgstr "Hello"
                """;

            PoDocument document = PoDocument.Parse(Source(Fragment) + Source(source));

            CollectionAssert.AreEqual(new[] { "#. said by the smith" }, document.TryGet("Greeting")!.Comments.ToArray());
            CollectionAssert.AreEqual(Array.Empty<string>(), document.TryGet(Bloodthirsty)!.Comments.ToArray());
        }

        [TestMethod]
        public void Set_ChangesOneLineAndNoOther()
        {
            string source = Source(Fragment);
            PoDocument document = PoDocument.Parse(source);

            Assert.IsTrue(document.Set(Bloodthirsty, "Bloodthirster"));

            int changed = OnlyChangedLine(source, document.Write());

            Assert.AreEqual("msgstr \"Bloodthirster\"", document.Write().Split('\n')[changed]);
        }

        /// <summary>Typing a translation is one thing the author did, so it is one thing to take back — and
        /// the step lands on the file that was there, not on the letter before last.</summary>
        [TestMethod]
        public void Set_MergesARunOnOneKey_AndUndoPutsTheFileBack()
        {
            string source = Source(Fragment);
            PoDocument document = PoDocument.Parse(source);

            document.Set(Bloodthirsty, "Blood");
            document.Set(Bloodthirsty, "Bloodthirster");

            Assert.AreEqual(1, document.History.Depth);

            document.History.Undo();

            Assert.AreEqual(source, document.Write());
            Assert.IsFalse(document.History.CanUndo);
        }

        [TestMethod]
        public void Set_OnAnotherKey_IsAnotherStep()
        {
            PoDocument document = PoDocument.Parse(Source(Fragment));

            document.Set(Bloodthirsty, "Blood");
            document.Set(SilentFury, "Quiet");

            Assert.AreEqual(2, document.History.Depth);

            document.History.Undo();

            Assert.AreEqual("Silent Fury", document.TryGet(SilentFury)?.MsgStr);
            Assert.AreEqual("Blood", document.TryGet(Bloodthirsty)?.MsgStr);
        }

        /// <summary>Writing what already stands is not an edit. A step filed for it would report the file as
        /// unsaved with nothing to save, and the undo the author then pressed would do nothing visible.</summary>
        [TestMethod]
        public void Set_OfWhatIsAlreadyThere_IsNotAStep()
        {
            PoDocument document = PoDocument.Parse(Source(Fragment));

            Assert.IsTrue(document.Set(Bloodthirsty, "Bloodthirsty"));
            Assert.AreEqual(0, document.History.Depth);
        }

        [TestMethod]
        public void Set_OnAKeyTheFileDoesNotHave_ChangesNothing()
        {
            string source = Source(Fragment);
            PoDocument document = PoDocument.Parse(source);

            Assert.IsFalse(document.Set(Missing, "anything"));
            Assert.AreEqual(source, document.Write());
            Assert.AreEqual(0, document.History.Depth);
        }

        /// <summary>A counted word has one translation per form and no single msgstr to write. Letting one
        /// through would put a line into the entry that gettext then reads instead of the forms.</summary>
        [TestMethod]
        public void Set_OnAPluralEntry_IsRefused()
        {
            string source = Source(Plurals);
            PoDocument document = PoDocument.Parse(source);

            Assert.IsTrue(document.TryGet(Turn)!.HasPlurals);
            Assert.IsFalse(document.Set(Turn, "move"));
            Assert.AreEqual(source, document.Write());
        }

        [TestMethod]
        public void Parse_KeepsCountedWordsWithTheirForms()
        {
            string source = Source(Plurals);
            PoDocument document = PoDocument.Parse(source);
            PoEntry turn = document.TryGet(Turn)!;

            Assert.AreEqual("turns", turn.MsgIdPlural);
            CollectionAssert.AreEqual(new[] { "turn", "turns" }, turn.PluralForms.Select(form => form.Text).ToArray());
            Assert.IsTrue(turn.IsTranslated);
            Assert.AreEqual(source, document.Write());
            Assert.AreEqual(0, document.Warnings.Count);
        }

        /// <summary>Escapes come back out the way they went in, and anything that is not an escape — Cyrillic
        /// here — is left alone rather than run through a numeric form the next reader cannot read.</summary>
        [TestMethod]
        public void Parse_ReadsAndWritesEscapesBothWays()
        {
            string source = Source(Escaped);
            PoDocument document = PoDocument.Parse(source);

            Assert.AreEqual("He said \"no\" \\ and left.\nНа русском\tтоже.", document.TryGet(Quoted)?.MsgStr);
            Assert.AreEqual(source, document.Write());

            document.Set(Quoted, "\\ \" \n Ещё");

            StringAssert.Contains(document.Write(), "msgstr \"\\\\ \\\" \\n Ещё\"");
        }

        /// <summary>A value the file laid out across lines is written back across lines. Collapsing it into
        /// one long line would rewrite an entry the author only edited the text of.</summary>
        [TestMethod]
        public void Parse_KeepsAMultilineValue_AndRewritesItInTheSameShape()
        {
            string source = Source(Multiline);
            PoDocument document = PoDocument.Parse(source);

            Assert.AreEqual("first line\nsecond line", document.TryGet(LongText)?.MsgStr);
            Assert.AreEqual(source, document.Write());

            document.Set(LongText, "one\ntwo\nthree");

            CollectionAssert.AreEqual(
                new[] { "msgid \"Long_Text\"", "msgstr \"\"", "\"one\\n\"", "\"two\\n\"", "\"three\"" },
                Lines(document).Skip(4).Take(5).ToArray());

            document.History.Undo();

            Assert.AreEqual(source, document.Write());
        }

        /// <summary>gettext takes the first of a repeated key and so does this. The second is kept in the file
        /// — the tool did not write it and has no business dropping it — but the author is told where it is.</summary>
        [TestMethod]
        public void Parse_ReportsARepeatedKey_AndTheFirstOneCounts()
        {
            string source = Source(Twice);
            PoDocument document = PoDocument.Parse(source);

            Assert.AreEqual(1, document.Warnings.Count);
            Assert.AreEqual(8, document.Warnings[0].Line);
            Assert.AreEqual("first", document.TryGet(Repeated)?.MsgStr);
            Assert.AreEqual(source, document.Write(), "the entry nothing addresses is still written back");
        }

        /// <summary>Taking out the entry a repeated key was pointing at hands the key to the copy still in the
        /// file. Left as it was, the key would answer with nothing while the file goes on writing it, and the
        /// next add of that key would put a third copy in.</summary>
        [TestMethod]
        public void Remove_OfARepeatedKey_HandsTheKeyToTheCopyStillInTheFile()
        {
            string source = Source(Twice);
            PoDocument document = PoDocument.Parse(source);

            Assert.IsTrue(document.Remove(Repeated));
            Assert.AreEqual("second", document.TryGet(Repeated)?.MsgStr);
            Assert.IsFalse(document.Add(Repeated, "third"), "the key is still written in the file");

            document.History.Undo();

            Assert.AreEqual("first", document.TryGet(Repeated)?.MsgStr);
            Assert.AreEqual(source, document.Write());
        }

        [TestMethod]
        public void Rename_OfARepeatedKey_HandsTheOldKeyToTheCopyStillInTheFile()
        {
            string source = Source(Twice);
            PoDocument document = PoDocument.Parse(source);

            Assert.IsTrue(document.Rename(Repeated, Renamed));
            Assert.AreEqual("first", document.TryGet(Renamed)?.MsgStr);
            Assert.AreEqual("second", document.TryGet(Repeated)?.MsgStr, "the copy nothing addressed is addressed now");

            document.History.Undo();

            Assert.AreEqual(source, document.Write());
            Assert.AreEqual("first", document.TryGet(Repeated)?.MsgStr);
        }

        /// <summary>What the file can still be opened with, said out loud. None of these stops the tool, and
        /// none of them is left for the author to notice from a diff.</summary>
        [TestMethod]
        public void Parse_ReportsWhatItKeptButCannotUse()
        {
            Assert.IsTrue(Warnings(string.Empty).Any(warning => warning.Contains("empty")), "an empty file");
            Assert.IsTrue(Warnings("msgid \"A\"\nmsgstr \"a\"\n").Any(warning => warning.Contains("header")), "a file without a header");
            Assert.IsTrue(Warnings("msgid \"A\"\n\nmsgid \"B\"\nmsgstr \"b\"\n").Any(warning => warning.Contains("no msgstr")), "an entry without a translation");
        }

        /// <summary>What the document cannot carry through a save it refuses to open, naming the line: a file
        /// the tool cannot keep whole is a file it has no business rewriting.</summary>
        [TestMethod]
        public void Parse_RefusesALineItCouldNotWriteBack()
        {
            AssertRefused("msgid \"A\nmsgstr \"a\"\n", "a quoted value is not closed");
            AssertRefused("msgid \"A\" and more\nmsgstr \"a\"\n", "after the closing quote");
            AssertRefused("msgid \"A\"\nmsgstr \"a \\q b\"\n", "unknown escape");
            AssertRefused("msgid \"A\"\nmsgstr \"a\"\n\n\"orphan\"\n", "no value is being continued");
            AssertRefused("msgid \"A\"\nmsgtext \"a\"\n", "not a gettext keyword");
            AssertRefused("msgctxt \"menu\"\nmsgid \"A\"\nmsgstr \"a\"\n", "msgctxt is not supported");
            AssertRefused("msgid \"A\"\nmsgstr \"a\"\nmsgstr \"b\"\n", "stands twice");
        }

        /// <summary>A comment where the translation should be belongs to neither the entry above it nor the
        /// one below, and the file says which entry the tool lost its place in.</summary>
        [TestMethod]
        public void Parse_RefusesACommentStandingInsideAnEntry()
        {
            AssertRefused("msgid \"A\"\n# a note\nmsgstr \"a\"\n", "inside the entry started at line 1");
        }

        /// <summary>A byte that is not UTF-8 is not quietly replaced. The replacement would stand in the
        /// source lines of an entry nobody edited and go to disk on the next save, so the file is refused
        /// while it can still be fixed by hand.</summary>
        [TestMethod]
        public void Load_RefusesAFileThatIsNotUtf8_RatherThanSubstituting()
        {
            byte[] valid = new UTF8Encoding(false).GetBytes(Source(Fragment));
            File.WriteAllBytes(_file, [.. valid[..40], 0xFF, .. valid[40..]]);

            FormatException refused = Assert.ThrowsException<FormatException>(() => PoDocument.Load(_file));

            StringAssert.Contains(refused.Message, "not valid UTF-8");
        }

        /// <summary>A section banner is written straight above the first entry of its section. The entry does
        /// not own it: deleting one key would take the whole heading with it, and the section after would run
        /// into the one before with nothing to say where it starts.</summary>
        [TestMethod]
        public void Remove_DoesNotCarryAwayTheSectionBannerAboveTheEntry()
        {
            string source = Source(Banner);
            PoDocument document = PoDocument.Parse(source);

            CollectionAssert.AreEqual(Array.Empty<string>(), document.TryGet(ArmorEssence)!.Comments.ToArray());

            Assert.IsTrue(document.Remove(ArmorEssence));

            List<string> lines = Lines(document);

            Assert.IsTrue(lines.Contains("# Essences"), "the heading is still there");
            Assert.AreEqual("# -----------------------", lines[^4]);
            Assert.AreEqual("msgid \"Crafting_Resource_Crit_Essence\"", lines[^3], "the next key of the section moved up under it");

            document.History.Undo();

            Assert.AreEqual(source, document.Write());
        }

        /// <summary>A comment written against one key goes with it — that is what the mark on it means.</summary>
        [TestMethod]
        public void Remove_CarriesAwayTheCommentsMarkedAgainstTheEntry()
        {
            const string obsolete = """
                msgid ""
                msgstr ""
                "Language: en\n"

                #~ msgid "Weapon_Old"
                #~ msgstr "Old"
                #. shown on the blade
                msgid "Weapon_Live"
                msgstr "Live"
                """;

            string source = Source(obsolete);
            PoDocument document = PoDocument.Parse(source);

            Assert.AreEqual(3, document.TryGet("Weapon_Live")!.Comments.Count);
            Assert.AreEqual(source, document.Write());

            Assert.IsTrue(document.Remove("Weapon_Live"));
            Assert.IsFalse(document.Write().Contains("Weapon_Old"), "the commented-out version went with the key");

            document.History.Undo();

            Assert.AreEqual(source, document.Write());
        }

        /// <summary>Two blank lines are how these files break one section off the next. A remove that ate
        /// them would weld the sections together in a diff nobody asked for.</summary>
        [TestMethod]
        public void Remove_KeepsTheWiderGapThatBreaksOneSectionOffTheNext()
        {
            string source = Source(Sections);
            PoDocument document = PoDocument.Parse(source);

            Assert.IsTrue(document.Remove(LastPraise));
            StringAssert.Contains(document.Write(), "\n\n\n", "the section break is still two blank lines");

            document.History.Undo();

            Assert.AreEqual(source, document.Write());
        }

        /// <summary>And an add must not step over that gap: the key was asked to follow a neighbour, not to
        /// join the section after it.</summary>
        [TestMethod]
        public void Add_AfterTheLastKeyOfASection_StaysOnThisSideOfTheBreak()
        {
            PoDocument document = PoDocument.Parse(Source(Sections));

            Assert.IsTrue(document.Add(Added, "Rusty", LastPraise));

            List<string> lines = Lines(document);
            int at = lines.IndexOf($"msgid \"{Added}\"");

            Assert.AreEqual("msgstr \"Done, and done well.\"", lines[at - 2], "it follows the key it was given");
            Assert.AreEqual(string.Empty, lines[at + 2]);
            Assert.AreEqual(string.Empty, lines[at + 3], "the two blank lines are still whole");
            Assert.AreEqual($"msgid \"{Scale}\"", lines[at + 4], "and the next section starts after them");
        }

        /// <summary>gettext lets a value be laid out under its keyword and a comment be indented. Reading
        /// them as anything else would refuse a hand-edited file the game itself loads.</summary>
        [TestMethod]
        public void Parse_ReadsIndentedContinuationsAndComments()
        {
            const string indented = """
                msgid ""
                msgstr ""
                  "Language: en\n"

                  #. laid out by hand
                msgid "Indented"
                msgstr ""
                  "first\n"
                  "second"
                """;

            string source = Source(indented);
            PoDocument document = PoDocument.Parse(source);

            Assert.AreEqual("first\nsecond", document.TryGet("Indented")?.MsgStr);
            CollectionAssert.AreEqual(new[] { "  #. laid out by hand" }, document.TryGet("Indented")!.Comments.ToArray());
            Assert.AreEqual(source, document.Write(), "an entry nobody edited keeps the spelling it had");
        }

        /// <summary>A key is how the rest of the tool holds an entry, so the step says which key became which
        /// — a selection left pointing at the old one is an undo that looks broken over data that is right.</summary>
        [TestMethod]
        public void Rename_SaysWhichKeyBecameWhich_InBothDirections()
        {
            PoDocument document = PoDocument.Parse(Source(Fragment));
            document.Rename(SilentFury, Renamed);

            var undone = document.History.Undo() as IIdChangingEdit;

            Assert.IsNotNull(undone);
            Assert.AreEqual(new IdSwap(Renamed, SilentFury), undone!.Swap(undoing: true));
            Assert.AreEqual(new IdSwap(SilentFury, Renamed), undone.Swap(undoing: false));
        }

        [TestMethod]
        public void Add_IntoAnEmptyDocument_MakesTheWholeFile()
        {
            PoDocument document = PoDocument.Parse(string.Empty);

            Assert.IsTrue(document.Add(Added, "Rusty"));
            Assert.AreEqual($"msgid \"{Added}\"\nmsgstr \"Rusty\"\n", document.Write());

            document.History.Undo();

            Assert.AreEqual(string.Empty, document.Write());
        }

        [TestMethod]
        public void Add_AfterANeighbour_LandsBesideIt_AndUndoTakesItBackOut()
        {
            string source = Source(Fragment);
            PoDocument document = PoDocument.Parse(source);

            Assert.IsTrue(document.Add(Added, "Rusty", SilentFury));

            List<string> lines = Lines(document);
            int neighbour = lines.IndexOf($"msgid \"{SilentFury}\"");

            Assert.AreEqual(string.Empty, lines[neighbour + 2], "the entries stay one blank line apart");
            Assert.AreEqual($"msgid \"{Added}\"", lines[neighbour + 3]);
            Assert.AreEqual("msgstr \"Rusty\"", lines[neighbour + 4]);
            Assert.AreEqual(string.Empty, lines[neighbour + 5]);

            document.History.Undo();

            Assert.AreEqual(source, document.Write());
        }

        [TestMethod]
        public void Add_WithoutANeighbour_GoesToTheEnd_WithABlankLineBeforeIt()
        {
            string source = Source(Fragment);
            PoDocument document = PoDocument.Parse(source);

            Assert.IsTrue(document.Add(Added, "Rusty"));
            Assert.AreEqual(source + $"\nmsgid \"{Added}\"\nmsgstr \"Rusty\"\n", document.Write());

            document.History.Undo();

            Assert.AreEqual(source, document.Write());
        }

        [TestMethod]
        public void Add_AKeyThatIsThere_OrAnUnknownNeighbour_IsRefused()
        {
            string source = Source(Fragment);
            PoDocument document = PoDocument.Parse(source);

            Assert.IsFalse(document.Add(Bloodthirsty, "again"));
            Assert.IsFalse(document.Add(Added, "Rusty", Missing));
            Assert.IsFalse(document.Add(string.Empty, "no key of its own"), "the header is not added by key");
            Assert.AreEqual(source, document.Write());
            Assert.AreEqual(0, document.History.Depth);
        }

        /// <summary>An entry takes its blank line with it, and the undo puts both back where they stood: a
        /// deletion that left the separator behind grows an empty line into the file every time.</summary>
        [TestMethod]
        public void Remove_TakesTheSeparatorWithIt_AndUndoRestoresThePlace()
        {
            string source = Source(Fragment);
            PoDocument document = PoDocument.Parse(source);

            Assert.IsTrue(document.Remove(SilentFury));
            Assert.IsNull(document.TryGet(SilentFury));

            string[] lines = document.Write().Split('\n');

            Assert.AreEqual(source.Split('\n').Length - 3, lines.Length, "the key, its translation and the blank line");
            Assert.IsFalse(document.Write().Contains("\n\n\n"), "no empty line is left where the entry stood");

            document.History.Undo();

            Assert.AreEqual(source, document.Write());
        }

        [TestMethod]
        public void Remove_TheLastEntry_TakesTheBlankLineBeforeIt()
        {
            string source = Source(Fragment);
            PoDocument document = PoDocument.Parse(source);

            Assert.IsTrue(document.Remove(Diamond));
            Assert.IsFalse(document.Write().EndsWith("\n\n"), "the file does not end on a blank line");

            document.History.Undo();

            Assert.AreEqual(source, document.Write());
        }

        [TestMethod]
        public void Remove_TheHeaderOrAMissingKey_IsRefused()
        {
            string source = Source(Fragment);
            PoDocument document = PoDocument.Parse(source);

            Assert.IsFalse(document.Remove(string.Empty), "the settings entry is not the tool's to delete");
            Assert.IsFalse(document.Remove(Missing));
            Assert.AreEqual(source, document.Write());
            Assert.AreEqual(0, document.History.Depth);
        }

        [TestMethod]
        public void Rename_KeepsThePlaceAndTheTranslation_AndUndoNamesItBack()
        {
            string source = Source(Fragment);
            PoDocument document = PoDocument.Parse(source);
            int at = source.Split('\n').ToList().IndexOf($"msgid \"{SilentFury}\"");

            Assert.IsTrue(document.Rename(SilentFury, Renamed));
            Assert.IsNull(document.TryGet(SilentFury));
            Assert.AreEqual("Silent Fury", document.TryGet(Renamed)?.MsgStr);
            Assert.AreEqual($"msgid \"{Renamed}\"", Lines(document)[at], "the key stays on the line it stood on");

            document.History.Undo();

            Assert.AreEqual(source, document.Write());
        }

        [TestMethod]
        public void Rename_ToAKeyThatIsTakenOrBlank_IsRefused()
        {
            string source = Source(Fragment);
            PoDocument document = PoDocument.Parse(source);

            Assert.IsFalse(document.Rename(SilentFury, Bloodthirsty));
            Assert.IsFalse(document.Rename(SilentFury, SilentFury), "the name it already has");
            Assert.IsFalse(document.Rename(SilentFury, string.Empty));
            Assert.IsFalse(document.Rename(string.Empty, Added), "the settings entry has no key to rename");
            Assert.IsFalse(document.Rename(Missing, Added));
            Assert.AreEqual(source, document.Write());
            Assert.AreEqual(0, document.History.Depth);
        }

        /// <summary>A step forward puts back exactly what the step back took away. Undo and redo run different
        /// halves of one command, and a redo that appended where the edit had inserted would be found only
        /// after the author walked the history and saved.</summary>
        [TestMethod]
        public void Redo_PutsBackWhatTheAddDid() => AssertRedoRepeats(document => document.Add(Added, "Rusty", SilentFury));

        [TestMethod]
        public void Redo_PutsBackWhatTheRemoveDid() => AssertRedoRepeats(document => document.Remove(SilentFury));

        [TestMethod]
        public void Redo_PutsBackWhatTheRenameDid() => AssertRedoRepeats(document => document.Rename(SilentFury, Renamed));

        [TestMethod]
        public void Redo_PutsBackWhatTheSetDid() => AssertRedoRepeats(document => document.Set(SilentFury, "Quiet Fury"));

        [TestMethod]
        public void Changed_CarriesTheKey_OnEditAndOnUndo()
        {
            PoDocument document = PoDocument.Parse(Source(Fragment));
            List<string> heard = [];
            document.Changed += key => heard.Add(key);

            document.Set(Bloodthirsty, "Blood");
            document.Rename(SilentFury, Renamed);

            CollectionAssert.AreEqual(
                new[] { Bloodthirsty, SilentFury, Renamed },
                heard,
                "a rename names both keys: one row stops naming anything, the other starts");

            document.History.Undo();
            document.History.Undo();

            CollectionAssert.AreEqual(
                new[] { Bloodthirsty, SilentFury, Renamed, Renamed, SilentFury, Bloodthirsty },
                heard);
        }

        [TestMethod]
        public void Save_WritesTheFile_AndTheHistoryCallsTheDocumentClean()
        {
            PoDocument document = PoDocument.Parse(Source(Fragment));
            document.Set(Bloodthirsty, "Blood");

            Assert.IsFalse(document.IsClean);

            document.Save(_file);

            Assert.IsTrue(document.IsClean);
            CollectionAssert.AreEqual(new UTF8Encoding(false).GetBytes(document.Write()), File.ReadAllBytes(_file));

            document.History.Undo();

            Assert.IsFalse(document.IsClean, "the catalog on screen is no longer the one on disk");
        }

        [TestMethod]
        public void Load_ReadsTheFileTheGameReads_AndSaveWritesItBackWithoutAMark()
        {
            string source = Source(Fragment);
            File.WriteAllBytes(_file, new UTF8Encoding(false).GetBytes(source));

            PoDocument document = PoDocument.Load(_file);

            Assert.IsFalse(document.HasByteOrderMark);

            document.Save(_file);

            CollectionAssert.AreEqual(new UTF8Encoding(false).GetBytes(source), File.ReadAllBytes(_file));
        }

        /// <summary>A mark Godot does not expect is kept rather than dropped — the tool is not the only writer
        /// of these files — and the author is told it is there.</summary>
        [TestMethod]
        public void Load_KeepsAByteOrderMark_AndSaysSoInTheWarnings()
        {
            byte[] marked = new UTF8Encoding(false).GetBytes((char)0xFEFF + Source(Fragment));
            File.WriteAllBytes(_file, marked);

            PoDocument document = PoDocument.Load(_file);

            Assert.IsTrue(document.HasByteOrderMark);
            Assert.IsTrue(document.Warnings.Any(warning => warning.Message.Contains("byte order mark")));

            document.Save(_file);

            CollectionAssert.AreEqual(marked, File.ReadAllBytes(_file));
        }

        /// <summary>A file written on Windows keeps its endings: normalising them would rewrite every line of
        /// a file the author changed one word in.</summary>
        [TestMethod]
        public void Parse_KeepsTheLineEndingsTheFileArrivedWith()
        {
            string source = Source(Fragment).Replace("\n", "\r\n");
            PoDocument document = PoDocument.Parse(source);

            Assert.AreEqual("\r\n", document.NewLine);
            Assert.AreEqual(source, document.Write());

            document.Set(Bloodthirsty, "Blood");

            StringAssert.Contains(document.Write(), "msgstr \"Blood\"\r\n");
        }

        [TestMethod]
        public void Parse_OfAFileThatDoesNotEndOnANewLine_DoesNotAddOne()
        {
            string source = Source(Fragment).TrimEnd('\n');
            PoDocument document = PoDocument.Parse(source);

            Assert.IsFalse(document.EndsWithNewLine);
            Assert.AreEqual(source, document.Write());
        }

        [TestMethod]
        public void Parse_OfAnEmptyFile_HasNothingInIt()
        {
            PoDocument document = PoDocument.Parse(string.Empty);

            Assert.AreEqual(0, document.Blocks.Count);
            Assert.IsNull(document.Header);
            Assert.AreEqual(string.Empty, document.Write());
        }

        /// <summary>One edit, walked back and forward: the file has to be the same on both ends of the walk.</summary>
        private static void AssertRedoRepeats(Func<PoDocument, bool> edit)
        {
            string source = Source(Fragment);
            PoDocument document = PoDocument.Parse(source);

            Assert.IsTrue(edit(document));

            string after = document.Write();

            document.History.Undo();

            Assert.AreEqual(source, document.Write());

            document.History.Redo();

            Assert.AreEqual(after, document.Write());
        }

        private static void AssertRefused(string source, string says)
        {
            FormatException refused = Assert.ThrowsException<FormatException>(() => PoDocument.Parse(source), says);

            StringAssert.Contains(refused.Message, says);
            StringAssert.StartsWith(refused.Message, "line ", "the author is told where to look");
        }

        private static IReadOnlyList<string> Warnings(string source) =>
            [.. PoDocument.Parse(source).Warnings.Select(warning => warning.Message)];

        /// <summary>The fragments are written here as the files are written on disk: line feeds only, and a
        /// newline at the end. Reading them out of the source file would tie the test to how it was checked out.</summary>
        private static string Source(string raw) => raw.Replace("\r\n", "\n") + "\n";

        private static List<string> Lines(PoDocument document) => [.. document.Write().Split('\n')];

        /// <summary>The one line that differs, or a failure naming how many did. An edit that quietly rewrote
        /// its neighbours would still pass every assertion about its own value.</summary>
        private static int OnlyChangedLine(string before, string after)
        {
            string[] was = before.Split('\n');
            string[] now = after.Split('\n');

            Assert.AreEqual(was.Length, now.Length, "the file changed its number of lines");

            int found = -1;

            for (int at = 0; at < was.Length; at++)
            {
                if (was[at] == now[at]) continue;

                Assert.AreEqual(-1, found, $"more than one line changed: {found} and {at}");
                found = at;
            }

            Assert.AreNotEqual(-1, found, "nothing changed");

            return found;
        }
    }
}

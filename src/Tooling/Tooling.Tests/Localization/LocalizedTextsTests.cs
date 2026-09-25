namespace Tooling.Tests.Localization
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Tooling.Catalogs;
    using Tooling.Editing.History;
    using Tooling.Localization;

    /// <summary>
    /// The text of one record, edited in both locales at once. What is easy to lose here is the half the
    /// author cannot see: the locale that did not get the key, the key that landed at the end of the file
    /// instead of beside the name it describes, and the rename that took a lookalike key along with it.
    /// <para>The two fixtures write their sections in opposite orders, as the shipped files do. A key
    /// has to reach the same neighbour in both of them, and an order the tests agree on would hide every
    /// way of getting there that only works while the files happen to match.</para>
    /// </summary>
    [TestClass]
    public class LocalizedTextsTests
    {
        private const string English = """
            msgid ""
            msgstr ""
            "Content-Type: text/plain; charset=UTF-8\n"
            "Language: en\n"

            # -----------------------
            # Equip items
            # -----------------------

            msgid "Weapon_Bloodthirsty"
            msgstr "Bloodthirsty"

            msgid "Weapon_Bloodthirsty_Description"
            msgstr "Drinks what it cuts."

            msgid "Weapon_Bloodthirsty_Extra"
            msgstr "Whets itself."

            msgid "Weapon_Keen"
            msgstr "Keen"

            # -----------------------
            # Interface
            # -----------------------

            msgid "UI_Close"
            msgstr "Close"
            """;

        private const string Russian = """
            msgid ""
            msgstr ""
            "Content-Type: text/plain; charset=UTF-8\n"
            "Language: ru\n"

            # -----------------------
            # Interface
            # -----------------------

            msgid "UI_Close"
            msgstr "Закрыть"

            # -----------------------
            # Equip items
            # -----------------------

            msgid "Weapon_Bloodthirsty"
            msgstr "Кровожадный"

            msgid "Weapon_Bloodthirsty_Description"
            msgstr ""

            msgid "Weapon_Bloodthirsty_Extra"
            msgstr "Точит себя."

            msgid "Weapon_Keen"
            msgstr "Острый"
            """;

        private const string En = "en";
        private const string Ru = "ru";

        private const string Name = "";
        private const string Description = "_Description";

        private const string Weapon = "Weapon_Bloodthirsty";
        private const string WeaponDescription = "Weapon_Bloodthirsty_Description";

        /// <summary>A key beginning with the record's id that the catalog words nothing from: what a rename
        /// by prefix would carry away and a rename by suffix leaves standing.</summary>
        private const string WeaponLookalike = "Weapon_Bloodthirsty_Extra";

        /// <summary>A record with a name and no description, standing in the middle of its section in one
        /// file and at the end of the other: what a key laid down at the end of a file is told apart by.</summary>
        private const string Keen = "Weapon_Keen";
        private const string KeenDescription = "Weapon_Keen_Description";

        private const string InterfaceBanner = "# Interface";

        private const string Rusty = "Weapon_Rusty";
        private const string RustyDescription = "Weapon_Rusty_Description";
        private const string Renamed = "Weapon_Sanguine";

        private const string Drinks = "Пьёт то, что режет.";

        /// <summary>What the host names the gesture that renames a record and its wording together.</summary>
        private const string RenameStep = "rename Weapon_Bloodthirsty → Weapon_Sanguine";

        private static readonly string[] s_suffixes = [Name, Description];

        private string _folder = string.Empty;

        /// <summary>The files the run under test is editing, so a test reads what is on screen rather than
        /// what is on disk: nothing here reaches disk until a save is asked for.</summary>
        private PoCatalogSet _set = null!;

        [TestInitialize]
        public void WriteTheFiles()
        {
            _folder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(_folder);
            Write(En, English);
            Write(Ru, Russian);
        }

        [TestCleanup]
        public void DeleteTheFiles()
        {
            if (Directory.Exists(_folder)) Directory.Delete(_folder, recursive: true);
        }

        /// <summary>The order the boxes are drawn in is the set's own, and the tool's own way of loading a
        /// set puts the authoring locale first. A view with an order of its own would draw one thing and
        /// write another the day a third locale arrives.</summary>
        [TestMethod]
        public void Locales_AreTheSetsOwnOrder_AndTheToolLoadsTheAuthoringOneFirst()
        {
            Assert.AreEqual(Ru, LocalizedTexts.AuthoringLocale);
            Assert.AreEqual(En, LocalizedTexts.ReferenceLocale);
            CollectionAssert.AreEqual(new[] { Ru, En }, LocalizedTexts.Load(_folder).Locales.ToArray());

            var reversed = new LocalizedTexts(PoCatalogSet.Load(_folder, En, Ru));

            CollectionAssert.AreEqual(new[] { En, Ru }, reversed.Locales.ToArray());
        }

        [TestMethod]
        public void Keys_WordsTheSuffixesInOrder_AndAnchorsEachOnTheOneBeforeIt()
        {
            IReadOnlyList<LocalizedTextKey> keys = LocalizedTexts.Keys(Weapon, s_suffixes);

            Assert.AreEqual(2, keys.Count);
            Assert.AreEqual(new LocalizedTextKey(Name, Weapon, null), keys[0]);
            Assert.AreEqual(new LocalizedTextKey(Description, WeaponDescription, Weapon), keys[1]);
            Assert.AreEqual(0, LocalizedTexts.Keys(string.Empty, s_suffixes).Count, "a record with no id words no key");
            Assert.AreEqual(0, LocalizedTexts.Keys(Weapon, []).Count, "a catalog wording nothing from its ids");

            Assert.AreEqual(
                new LocalizedTextKey(Name, Weapon, Keen),
                LocalizedTexts.Keys(Weapon, s_suffixes, Keen)[0],
                "the first key follows the neighbour it was given");
        }

        /// <summary>Where a fresh record's keys go: under everything the record before it wrote. Under its
        /// name alone they would come between that name and the description belonging to it.</summary>
        [TestMethod]
        public void AnchorOf_IsTheLastKeyTheNeighbourActuallyWrote()
        {
            LocalizedTexts texts = Load();

            Assert.AreEqual(WeaponDescription, texts.AnchorOf(Weapon, s_suffixes));
            Assert.AreEqual(Keen, texts.AnchorOf(Keen, s_suffixes), "the neighbour wrote no description");
            Assert.IsNull(texts.AnchorOf(Rusty, s_suffixes), "a neighbour that wrote nothing at all");
            Assert.IsNull(texts.AnchorOf(null, s_suffixes), "the first record of a catalog has no neighbour");
        }

        [TestMethod]
        public void Read_AnswersPerLocale_AndTellsAnAbsentKeyFromAnEmptyOne()
        {
            LocalizedTexts texts = Load();

            Assert.AreEqual("Кровожадный", texts.Read(Ru, Weapon));
            Assert.AreEqual("Bloodthirsty", texts.Read(En, Weapon));
            Assert.AreEqual(string.Empty, texts.Read(Ru, WeaponDescription), "the key is there and says nothing");
            Assert.IsNull(texts.Read(Ru, Rusty), "no such key at all");
            Assert.IsNull(texts.Read(Ru, string.Empty), "the keyless entry is the file's settings, not a text");
            Assert.ThrowsException<KeyNotFoundException>(() => texts.Read("de", Weapon));
        }

        [TestMethod]
        public void Write_ChangesTheOneLocaleItWasAskedFor()
        {
            LocalizedTexts texts = Load();

            Assert.IsTrue(texts.Write(Ru, WeaponDescription, Drinks));

            Assert.AreEqual(Drinks, texts.Read(Ru, WeaponDescription));
            Assert.AreEqual("Drinks what it cuts.", texts.Read(En, WeaponDescription), "the other locale was not touched");
        }

        /// <summary>The first letter of a text lays the key down in every locale, so the locale nobody wrote
        /// in still holds the key and shows up as untranslated rather than as a key that does not exist.</summary>
        [TestMethod]
        public void Write_OfAKeyNobodyHas_LaysItDownInEveryLocale()
        {
            LocalizedTexts texts = Load();

            Assert.IsTrue(texts.Write(Ru, Rusty, "Ржавый"));

            Assert.AreEqual("Ржавый", texts.Read(Ru, Rusty));
            Assert.AreEqual(string.Empty, texts.Read(En, Rusty), "the locale nobody wrote in gets the key empty");
        }

        /// <summary>A description lands beside the name it describes, in both files and inside the section
        /// that name stands in — which is a different section of each file. A key that went to the end of
        /// the file instead is one the author has to go and move by hand, in each locale separately.</summary>
        [TestMethod]
        public void Write_OfADescriptionNobodyHas_LaysItBesideTheNameItDescribes()
        {
            LocalizedTexts texts = Load();
            LocalizedTextKey description = LocalizedTexts.Keys(Keen, s_suffixes)[1];

            Assert.AreEqual(KeenDescription, description.Key);
            Assert.IsTrue(texts.Write(Ru, description.Key, "Режет взгляд.", description.After));
            Assert.AreEqual(string.Empty, texts.Read(En, KeenDescription), "the other locale got the key too");

            foreach (string locale in texts.Locales)
            {
                List<string> lines = [.. Written(locale).Split('\n')];
                int at = lines.IndexOf($"msgid \"{KeenDescription}\"");

                Assert.IsTrue(at > 0, $"{locale} holds the description");
                Assert.AreEqual($"msgid \"{Keen}\"", lines[at - 3], $"{locale} stands it right after the name");
            }

            List<string> english = [.. Written(En).Split('\n')];

            Assert.IsTrue(
                english.IndexOf($"msgid \"{KeenDescription}\"") < english.IndexOf(InterfaceBanner),
                "and above the section that follows in the file that has one");
        }

        /// <summary>A locale that lacks a key gets it beside the same neighbour the locale that has it put
        /// it after — and the two files write their sections in different orders, so the neighbour is what
        /// answers, never the place.</summary>
        [TestMethod]
        public void Write_IntoALocaleThatLacksTheKey_LaysItDownBesideTheSameNeighbour()
        {
            LocalizedTexts texts = Load();

            _set.Get(En).Add(Rusty, "Rusty", Weapon);

            Assert.IsTrue(texts.Write(Ru, Rusty, "Ржавый"));
            Assert.AreEqual("Rusty", texts.Read(En, Rusty), "the locale that had it kept what it said");

            List<string> lines = [.. Written(Ru).Split('\n')];
            int at = lines.IndexOf($"msgid \"{Rusty}\"");

            Assert.IsTrue(at > 0);
            Assert.AreEqual($"msgid \"{Weapon}\"", lines[at - 3], "it followed the neighbour the other locale gave it");
        }

        /// <summary>A description written before its own name still ends up under it. Its own anchor is a
        /// key nobody has written yet, and without falling back through the family the pair would arrive in
        /// the file back to front.</summary>
        [TestMethod]
        public void Write_OfADescriptionBeforeItsName_LeavesThePairInOrder()
        {
            LocalizedTexts texts = Load();
            IReadOnlyList<LocalizedTextKey> family = LocalizedTexts.Keys(Rusty, s_suffixes, texts.AnchorOf(Keen, s_suffixes));

            Assert.IsTrue(texts.Write(Ru, family, 1, "Ест собственное лезвие."));
            Assert.IsTrue(texts.Write(Ru, family, 0, "Ржавый"));

            foreach (string locale in texts.Locales)
            {
                List<string> lines = [.. Written(locale).Split('\n')];
                int at = lines.IndexOf($"msgid \"{RustyDescription}\"");

                Assert.IsTrue(at > 0, $"{locale} holds the description");

                // Adjacency and not merely order: a description sent to the end of the file still stands
                // below its name, and in the file whose sections run the other way it stands below every
                // other record's too.
                Assert.AreEqual($"msgid \"{Rusty}\"", lines[at - 3], $"{locale} stands the pair together");
            }
        }

        /// <summary>Nothing typed is nothing to write. A panel writing every empty box down would put the
        /// whole family of keys of every record merely looked at into both files.</summary>
        [TestMethod]
        public void Write_OfNothingUnderAKeyNobodyHas_LaysDownNothing()
        {
            LocalizedTexts texts = Load();

            Assert.IsFalse(texts.Write(Ru, Rusty, string.Empty));
            Assert.IsFalse(texts.Write(Ru, string.Empty, "Ржавый"), "a field naming no key at all");

            Assert.IsNull(texts.Read(Ru, Rusty));
            Assert.IsNull(texts.Read(En, Rusty));
            Assert.IsFalse(texts.IsDirty);
        }

        /// <summary>Clearing a key that is there is an edit like any other: an empty box over a written key
        /// is a translation the author took back, not a refusal to write.</summary>
        [TestMethod]
        public void Write_OfNothingUnderAKeyThatIsThere_ClearsIt()
        {
            LocalizedTexts texts = Load();

            Assert.IsTrue(texts.Write(Ru, Weapon, string.Empty));
            Assert.AreEqual(string.Empty, texts.Read(Ru, Weapon));
            Assert.IsTrue(texts.IsDirty);
        }

        [TestMethod]
        public void RenameRecord_NamesEverySuffixInEveryLocale_AndNothingElse()
        {
            LocalizedTexts texts = Load();

            Assert.AreEqual(new LocalizedRename(2, null), texts.RenameRecord(Weapon, Renamed, s_suffixes));

            foreach (string locale in texts.Locales)
            {
                Assert.IsNull(texts.Read(locale, Weapon));
                Assert.IsNull(texts.Read(locale, WeaponDescription));
                Assert.IsNotNull(texts.Read(locale, Renamed));
                Assert.IsNotNull(texts.Read(locale, Renamed + Description));
                Assert.IsNotNull(texts.Read(locale, WeaponLookalike), "a key the catalog words nothing from stays");
            }

            Assert.AreEqual("Кровожадный", texts.Read(Ru, Renamed), "the text went with the key");
        }

        /// <summary>All of a record's keys move, or none of them do. Renaming the ones whose new names are
        /// free leaves a record read under one id for its name and another for its description — and the
        /// author, looking at one box that answered and one that did not, has no gesture that puts it back.</summary>
        [TestMethod]
        public void RenameRecord_ToANameOneOfTheKeysIsTakenBy_MovesNothing_AndSaysWhichKey()
        {
            LocalizedTexts texts = Load();

            // The new name itself is free; the name its description would take is not.
            _set.Get(En).Add(Renamed + Description, "Taken", Weapon);

            Assert.AreEqual(new LocalizedRename(0, Renamed + Description), texts.RenameRecord(Weapon, Renamed, s_suffixes));

            foreach (string locale in texts.Locales)
            {
                Assert.IsNotNull(texts.Read(locale, Weapon), "the name stayed where it was");
                Assert.IsNotNull(texts.Read(locale, WeaponDescription), "and so did the description");
                Assert.IsNull(texts.Read(locale, Renamed), "nothing was written under the new name");
            }
        }

        [TestMethod]
        public void RenameRecord_WithNothingToMove_MovesNothing()
        {
            LocalizedTexts texts = Load();

            Assert.AreEqual(0, texts.RenameRecord(Rusty, Renamed, s_suffixes).Renamed, "no locale has those keys");
            Assert.AreEqual(0, texts.RenameRecord(Weapon, Weapon, s_suffixes).Renamed, "the same word twice");
            Assert.AreEqual(0, texts.RenameRecord(Weapon, string.Empty, s_suffixes).Renamed);
            Assert.AreEqual(0, texts.RenameRecord(Weapon, Renamed, []).Renamed, "a catalog wording nothing from its ids");
            Assert.IsFalse(texts.IsDirty);
        }

        [TestMethod]
        public void IsDirty_FollowsTheFilesOwnHistories_AndSaveAllWritesTheChangedOnes()
        {
            LocalizedTexts texts = Load();

            Assert.IsFalse(texts.IsDirty);

            texts.Write(Ru, WeaponDescription, Drinks);

            Assert.IsTrue(texts.IsDirty);
            Assert.AreEqual(1, texts.DirtyCount, "the locale nobody wrote in has nothing to save");

            CatalogSaveResult result = texts.SaveAll();

            Assert.AreEqual(0, result.Notes.Count);
            CollectionAssert.AreEqual(new[] { Path.Combine(_folder, Ru + PoCatalogSet.FileExtension) }, result.Saved.ToArray());
            Assert.IsFalse(texts.IsDirty);
            Assert.IsTrue(ReadBack(Ru).Contains(Drinks, StringComparison.Ordinal));
            Assert.AreEqual(English.Replace("\r\n", "\n") + "\n", ReadBack(En), "the locale nobody wrote in was left alone");
        }

        /// <summary>Every locale carries its own stack, so laying a key down is a step per file. The run is
        /// clean only once every file is, and one press of undo cannot make it so.</summary>
        [TestMethod]
        public void Write_IsAStepInEveryFileItTouched()
        {
            LocalizedTexts texts = Load();

            texts.Write(Ru, Rusty, "Ржавый");

            Assert.AreEqual(1, Depth(En), "the key was laid down there too");
            Assert.AreEqual(2, Depth(Ru), "laid down, then written in");

            _set.Get(En).History.Undo();

            Assert.IsNull(texts.Read(En, Rusty));
            Assert.IsTrue(texts.IsDirty, "the other file is still holding the word");
        }

        /// <summary>A run of keystrokes in one box is one step to take back; leaving the box ends it. Without
        /// that, every text the author writes after the first would be absorbed into the first one's step.</summary>
        [TestMethod]
        public void Seal_EndsTheRunSoTheNextTextIsAStepOfItsOwn()
        {
            LocalizedTexts texts = Load();

            texts.Write(Ru, Weapon, "Кров");
            texts.Write(Ru, Weapon, "Кровавый");

            Assert.AreEqual(1, Depth(Ru), "one box, one step");

            texts.Seal();
            texts.Write(Ru, Weapon, "Кровожадный клинок");

            Assert.AreEqual(2, Depth(Ru));
        }

        /// <summary>
        /// A record renamed is one gesture: the id in its own file and the keys of every locale worded from
        /// it. Filed as one step — the way the host opens it around the rename — one press of undo puts back
        /// the id and the keys of both locales together.
        /// <para>Without the step, the same rename is two moves of the wording plus the id, and an author
        /// taking back what he just did would be left with a record read under one word in its file and
        /// under another in the catalogs — a state nothing on screen shows him.</para>
        /// </summary>
        [TestMethod]
        public void RenameRecord_InsideOneStep_IsTakenBackWholeInEveryLocale()
        {
            EditHistory history = new();
            LocalizedTexts texts = Load(history);

            using (history.Group(RenameStep))
            {
                Assert.AreEqual(new LocalizedRename(2, null), texts.RenameRecord(Weapon, Renamed, s_suffixes));
            }

            Assert.AreEqual(1, history.Depth, "one gesture over two locales is one step");
            Assert.AreEqual(RenameStep, history.NextUndo);

            history.Undo();

            foreach (string locale in texts.Locales)
            {
                Assert.IsNotNull(texts.Read(locale, Weapon), "the name is read under the old id again");
                Assert.IsNotNull(texts.Read(locale, WeaponDescription));
                Assert.IsNull(texts.Read(locale, Renamed));
            }

            Assert.IsFalse(texts.IsDirty, "the files are back where they were opened");
        }

        [TestMethod]
        public void Load_OfAFolderWithoutTheFiles_SaysSo()
        {
            string empty = Path.Combine(_folder, "empty");
            Directory.CreateDirectory(empty);

            Assert.ThrowsException<FileNotFoundException>(() => LocalizedTexts.Load(empty));
        }

        private LocalizedTexts Load(EditHistory? history = null)
        {
            _set = PoCatalogSet.Load(_folder, history, LocalizedTexts.AuthoringLocale, LocalizedTexts.ReferenceLocale);

            return new LocalizedTexts(_set);
        }

        private int Depth(string locale) => _set.Get(locale).History.Depth;

        private string Written(string locale) => _set.Get(locale).Write();

        private string ReadBack(string locale) =>
            new UTF8Encoding(false).GetString(File.ReadAllBytes(Path.Combine(_folder, locale + PoCatalogSet.FileExtension)));

        private void Write(string locale, string source) => File.WriteAllBytes(
            Path.Combine(_folder, locale + PoCatalogSet.FileExtension),
            new UTF8Encoding(false).GetBytes(source.Replace("\r\n", "\n") + "\n"));
    }
}

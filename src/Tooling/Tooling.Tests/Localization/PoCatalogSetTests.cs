namespace Tooling.Tests.Localization
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Tooling.Localization;

    /// <summary>
    /// Two locales edited together. A key is one thing to the author and two lines on disk, and the tests
    /// here are about the half that is easy to lose: the file that did not get the key, the file where it
    /// landed in another section, and the undo that has to be pressed once per file because the files are two.
    /// </summary>
    [TestClass]
    public class PoCatalogSetTests
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
            # Equip items
            # -----------------------

            msgid "Weapon_Bloodthirsty"
            msgstr "Кровожадный"

            msgid "Weapon_Bloodthirsty_Description"
            msgstr ""

            # -----------------------
            # Interface
            # -----------------------

            msgid "UI_Close"
            msgstr "Закрыть"
            """;

        private const string En = "en";
        private const string Ru = "ru";
        private const string Weapon = "Weapon_Bloodthirsty";
        private const string WeaponDescription = "Weapon_Bloodthirsty_Description";
        private const string Close = "UI_Close";
        private const string Added = "Weapon_Rusty";
        private const string Renamed = "Weapon_Sanguine";
        private const string WeaponPrefix = "Weapon_";

        private string _folder = string.Empty;

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

        [TestMethod]
        public void Load_ReadsALocalePerFile()
        {
            PoCatalogSet set = Load();

            CollectionAssert.AreEqual(new[] { En, Ru }, set.Locales.ToArray());
            Assert.AreEqual("Bloodthirsty", set.Get(En).TryGet(Weapon)?.MsgStr);
            Assert.AreEqual("Кровожадный", set.Get(Ru).TryGet(Weapon)?.MsgStr);
            Assert.ThrowsException<KeyNotFoundException>(() => set.Get("de"));
        }

        /// <summary>The number the author is after when asking how far a locale has got: keys that actually
        /// say something, not keys that exist.</summary>
        [TestMethod]
        public void Coverage_CountsOnlyKeysThatSaySomething()
        {
            PoCatalogSet set = Load();

            IReadOnlyDictionary<string, PoCoverage> everything = set.Coverage(string.Empty);
            IReadOnlyDictionary<string, PoCoverage> weapons = set.Coverage(WeaponPrefix);

            Assert.AreEqual(new PoCoverage(3, 3), everything[En]);
            Assert.AreEqual(new PoCoverage(2, 3), everything[Ru], "the empty description does not count");
            Assert.AreEqual(new PoCoverage(2, 2), weapons[En]);
            Assert.AreEqual(new PoCoverage(1, 2), weapons[Ru]);
        }

        /// <summary>Coverage and the missing list are two readings of one set of keys. Counted apart they
        /// drift the moment a file writes a key twice, and the author is shown a total that is not a total.</summary>
        [TestMethod]
        public void Coverage_AndMissingIn_CountTheSameKeys()
        {
            PoCatalogSet set = Load();
            set.Get(En).Add("UI_Open", "Open", Close);

            PoCoverage russian = set.Coverage(string.Empty)[Ru];

            Assert.AreEqual(4, russian.Total, "three shared keys and the one only English has");
            Assert.AreEqual(russian.Total - russian.Translated, set.MissingIn(Ru).Count);
        }

        [TestMethod]
        public void MissingIn_NamesWhatIsAbsentAndWhatIsEmpty()
        {
            PoCatalogSet set = Load();
            set.Get(En).Add("UI_Open", "Open", Close);

            CollectionAssert.AreEqual(new[] { WeaponDescription, "UI_Open" }, set.MissingIn(Ru).ToArray());
            CollectionAssert.AreEqual(Array.Empty<string>(), set.MissingIn(En).ToArray());
        }

        /// <summary>A key added to one file has to land in the same section of the other, or the two files
        /// drift apart section by section until a diff of them is unreadable.</summary>
        [TestMethod]
        public void EnsureKey_PutsTheKeyAfterTheSameNeighbourEverywhere()
        {
            PoCatalogSet set = Load();
            set.Get(En).Add(Added, "Rusty", Weapon);

            Assert.IsTrue(set.EnsureKey(Added));

            foreach (string locale in set.Locales)
            {
                List<string> lines = [.. set.Get(locale).Write().Split('\n')];
                int at = lines.IndexOf($"msgid \"{Added}\"");

                Assert.AreEqual($"msgid \"{Weapon}\"", lines[at - 3], "it stands right after its neighbour");
                Assert.IsTrue(at < lines.IndexOf("# Interface"), "and still above the next section");
            }

            Assert.AreEqual(string.Empty, set.Get(Ru).TryGet(Added)?.MsgStr, "an untranslated key is added empty");
        }

        [TestMethod]
        public void EnsureKey_OfAKeyEveryLocaleHas_ChangesNothing()
        {
            PoCatalogSet set = Load();

            Assert.IsTrue(set.EnsureKey(Weapon));
            Assert.IsTrue(set.Locales.All(locale => set.Get(locale).IsClean));
        }

        /// <summary>With no neighbour the locales agree on, the key goes to the end rather than into a section
        /// picked by whichever file happened to be read first — and the answer says so, because a key that
        /// landed outside its section is something the author has to go and move.</summary>
        [TestMethod]
        public void EnsureKey_WithoutASharedNeighbour_AddsToTheEnd_AndSaysItCouldNotPlaceIt()
        {
            PoCatalogSet set = Load();
            set.Get(En).Add("UI_Open", "Open", Close);
            set.Get(En).Add(Added, "Rusty", "UI_Open");

            Assert.IsFalse(set.EnsureKey(Added));
            Assert.IsNotNull(set.Get(Ru).TryGet(Added));
            Assert.IsTrue(set.Get(Ru).Write().EndsWith($"msgid \"{Added}\"\nmsgstr \"\"\n", StringComparison.Ordinal));
        }

        /// <summary>Every file keeps its own history, so a shared operation is one step per file. Pressing
        /// undo once and calling the set clean would leave the other file changed on disk.</summary>
        [TestMethod]
        public void EnsureKey_IsOneStepInEachFileThatNeededIt()
        {
            PoCatalogSet set = Load();
            string before = set.Get(Ru).Write();
            set.Get(En).Add(Added, "Rusty", Weapon);

            set.EnsureKey(Added);

            Assert.AreEqual(1, set.Get(Ru).History.Depth);
            Assert.AreEqual(1, set.Get(En).History.Depth, "the file that already had the key gains no step of its own");

            set.Get(Ru).History.Undo();

            Assert.AreEqual(before, set.Get(Ru).Write());
        }

        [TestMethod]
        public void RenameKey_NamesTheKeyAgainInEveryLocale_AndUndoNamesItBack()
        {
            PoCatalogSet set = Load();
            Dictionary<string, string> before = set.Locales.ToDictionary(locale => locale, locale => set.Get(locale).Write());

            Assert.IsTrue(set.RenameKey(Weapon, Renamed));

            foreach (string locale in set.Locales)
            {
                Assert.IsNull(set.Get(locale).TryGet(Weapon));
                Assert.IsNotNull(set.Get(locale).TryGet(Renamed));

                set.Get(locale).History.Undo();

                Assert.AreEqual(before[locale], set.Get(locale).Write());
            }
        }

        /// <summary>A key only one locale has is renamed there and reported done: the locale that never had
        /// it has nothing to rename, and calling that a failure would stop the rename the author asked for.</summary>
        [TestMethod]
        public void RenameKey_SkipsTheLocalesThatNeverHadTheKey()
        {
            PoCatalogSet set = Load();
            set.Get(En).Add(Added, "Rusty", Weapon);

            Assert.IsTrue(set.RenameKey(Added, Renamed));
            Assert.IsNotNull(set.Get(En).TryGet(Renamed));
            Assert.IsNull(set.Get(Ru).TryGet(Renamed));
            Assert.AreEqual(0, set.Get(Ru).History.Depth);
        }

        [TestMethod]
        public void RenameKey_ToAKeyThatIsTakenSomewhere_SaysSo()
        {
            PoCatalogSet set = Load();

            Assert.IsFalse(set.RenameKey(Weapon, Close));
            Assert.IsFalse(set.RenameKey("Weapon_Nothing", Renamed), "a key no locale has");
        }

        /// <summary>The name has to be free in every file before any file is touched. Renaming where it fits
        /// and refusing where it does not leaves the catalogs holding different sets of keys — the one state
        /// nothing downstream can tell apart from a translation that was simply never written.</summary>
        [TestMethod]
        public void RenameKey_IsRefusedWholeWhenTheNameIsFreeInOnlyOneLocale()
        {
            PoCatalogSet set = Load();
            set.Get(Ru).Remove(Close);

            Dictionary<string, string> before = set.Locales.ToDictionary(locale => locale, locale => set.Get(locale).Write());

            Assert.IsFalse(set.RenameKey(Weapon, Close), "the name is still taken in English");

            foreach (string locale in set.Locales)
            {
                Assert.IsNotNull(set.Get(locale).TryGet(Weapon), "no locale was renamed");
                Assert.AreEqual(before[locale], set.Get(locale).Write());
            }
        }

        [TestMethod]
        public void SaveAll_WritesEveryLocaleBack_AndTheDocumentsCallThemselvesClean()
        {
            PoCatalogSet set = Load();
            set.Get(Ru).Set(WeaponDescription, "Пьёт то, что режет.");

            Assert.IsFalse(set.Get(Ru).IsClean);

            set.SaveAll();

            foreach (string locale in set.Locales)
            {
                Assert.IsTrue(set.Get(locale).IsClean);
                Assert.AreEqual(set.Get(locale).Write(), ReadBack(locale));
            }

            Assert.AreEqual(Path.Combine(_folder, Ru + PoCatalogSet.FileExtension), set.PathOf(Ru));
        }

        [TestMethod]
        public void RemoveKey_TakesTheKeyOutOfEveryLocale_AndUndoPutsItBack()
        {
            PoCatalogSet set = Load();
            Dictionary<string, string> before = set.Locales.ToDictionary(locale => locale, locale => set.Get(locale).Write());

            Assert.IsTrue(set.RemoveKey(WeaponDescription));

            foreach (string locale in set.Locales)
            {
                Assert.IsNull(set.Get(locale).TryGet(WeaponDescription));

                set.Get(locale).History.Undo();

                Assert.AreEqual(before[locale], set.Get(locale).Write());
            }
        }

        [TestMethod]
        public void Load_OfAFolderWithoutTheFile_SaysWhichOneIsMissing()
        {
            Assert.ThrowsException<FileNotFoundException>(() => PoCatalogSet.Load(_folder, En, "de"));
            Assert.ThrowsException<ArgumentException>(() => PoCatalogSet.Load(_folder), "a set of no locales");
            Assert.ThrowsException<ArgumentException>(() => PoCatalogSet.Load(_folder, En, En), "the same locale twice");
        }

        private PoCatalogSet Load() => PoCatalogSet.Load(_folder, En, Ru);

        private string ReadBack(string locale) =>
            new UTF8Encoding(false).GetString(File.ReadAllBytes(Path.Combine(_folder, locale + PoCatalogSet.FileExtension)));

        private void Write(string locale, string source) => File.WriteAllBytes(
            Path.Combine(_folder, locale + PoCatalogSet.FileExtension),
            new UTF8Encoding(false).GetBytes(source.Replace("\r\n", "\n") + "\n"));
    }
}

namespace LastBreathTest.BattleSystemTests
{
    using Core.Save;
    using Newtonsoft.Json.Linq;

    [TestClass]
    public class SaveManagerTests
    {
        private LoadScope _loadScope = null!;
        private SaveManager _manager = null!;

        [TestInitialize]
        public void Setup()
        {
            _loadScope = new LoadScope();
            _manager = new SaveManager(_loadScope);
        }

        [TestMethod]
        public void CaptureWritesSectionPerParticipant()
        {
            _manager.Register(new FakeParticipant("mastery", version: 2) { OnCapture = () => new JObject { ["level"] = 7 } });

            var file = _manager.Capture(new SaveMetadata { Name = "Slot" });

            Assert.AreEqual(SaveFile.CurrentFormatVersion, file.FormatVersion);
            Assert.AreEqual(2, file.Sections["mastery"].Version);
            Assert.AreEqual(7, (int)file.Sections["mastery"].Data["level"]!);
        }

        [TestMethod]
        public void CapturePreservesForeignSectionsFromPrevious()
        {
            // Battle rewrites the shared file: crafting's section must survive untouched.
            var previous = new SaveFile();
            previous.Sections["crafting"] = new SaveSection { Version = 3, Data = new JObject { ["recipes"] = 5 } };
            _manager.Register(new FakeParticipant("mastery"));

            var file = _manager.Capture(new SaveMetadata(), previous);

            Assert.AreEqual(3, file.Sections["crafting"].Version);
            Assert.AreEqual(5, (int)file.Sections["crafting"].Data["recipes"]!);
            Assert.IsTrue(file.Sections.ContainsKey("mastery"));
        }

        [TestMethod]
        public void CaptureOverwritesOwnSectionFromPrevious()
        {
            var previous = new SaveFile();
            previous.Sections["mastery"] = new SaveSection { Version = 1, Data = new JObject { ["level"] = 1 } };
            _manager.Register(new FakeParticipant("mastery", version: 2) { OnCapture = () => new JObject { ["level"] = 9 } });

            var file = _manager.Capture(new SaveMetadata(), previous);

            Assert.AreEqual(9, (int)file.Sections["mastery"].Data["level"]!);
            Assert.AreEqual(2, file.Sections["mastery"].Version);
        }

        [TestMethod]
        public void RestoreFollowsRestoreOrderNotRegistrationOrder()
        {
            var calls = new List<string>();
            var vitals = new FakeParticipant("vitals", restoreOrder: RestoreOrder.Vitals) { OnRestore = _ => calls.Add("vitals") };
            var items = new FakeParticipant("items", restoreOrder: RestoreOrder.Items) { OnRestore = _ => calls.Add("items") };
            _manager.Register(vitals); // registered first, must restore last
            _manager.Register(items);

            _manager.Restore(FileWithSections("vitals", "items"));

            CollectionAssert.AreEqual(new[] { "items", "vitals" }, calls);
        }

        [TestMethod]
        public void RestoreSkipsMissingSections()
        {
            bool restored = false;
            bool failed = false;
            _manager.Register(new FakeParticipant("inventory") { OnRestore = _ => restored = true });
            _manager.SectionRestoreFailed += (_, _) => failed = true;

            _manager.Restore(new SaveFile()); // old save without the new section: not an error

            Assert.IsFalse(restored);
            Assert.IsFalse(failed);
        }

        [TestMethod]
        public void FailingSectionDoesNotStopOthers()
        {
            var failures = new List<string>();
            bool secondRestored = false;
            _manager.Register(new FakeParticipant("broken", restoreOrder: 0) { OnRestore = _ => throw new InvalidOperationException("corrupt") });
            _manager.Register(new FakeParticipant("healthy", restoreOrder: 10) { OnRestore = _ => secondRestored = true });
            _manager.SectionRestoreFailed += (section, _) => failures.Add(section);

            _manager.Restore(FileWithSections("broken", "healthy"));

            Assert.IsTrue(secondRestored);
            CollectionAssert.AreEqual(new[] { "broken" }, failures);
        }

        [TestMethod]
        public void SectionNewerThanParticipantIsSkippedAndReported()
        {
            bool restored = false;
            var failures = new List<string>();
            _manager.Register(new FakeParticipant("mastery", version: 1) { OnRestore = _ => restored = true });
            _manager.SectionRestoreFailed += (section, _) => failures.Add(section);

            var file = new SaveFile();
            file.Sections["mastery"] = new SaveSection { Version = 2, Data = new JObject() };
            _manager.Restore(file);

            Assert.IsFalse(restored);
            CollectionAssert.AreEqual(new[] { "mastery" }, failures);
        }

        [TestMethod]
        public void RestorePassesSavedVersionForMigration()
        {
            int seenVersion = -1;
            var participant = new FakeParticipant("mastery", version: 3);
            participant.OnRestoreWithVersion = (_, version) => seenVersion = version;
            _manager.Register(participant);

            var file = new SaveFile();
            file.Sections["mastery"] = new SaveSection { Version = 1, Data = new JObject() };
            _manager.Restore(file);

            Assert.AreEqual(1, seenVersion);
        }

        [TestMethod]
        public void RestoreThrowsOnNewerFormatVersion()
        {
            var file = new SaveFile { FormatVersion = SaveFile.CurrentFormatVersion + 1 };

            Assert.ThrowsException<InvalidOperationException>(() => _manager.Restore(file));
        }

        [TestMethod]
        public void LoadScopeIsActiveExactlyDuringRestore()
        {
            bool wasLoading = false;
            _manager.Register(new FakeParticipant("any") { OnRestore = _ => wasLoading = _loadScope.IsLoading });

            Assert.IsFalse(_loadScope.IsLoading);
            _manager.Restore(FileWithSections("any"));

            Assert.IsTrue(wasLoading);
            Assert.IsFalse(_loadScope.IsLoading);
        }

        [TestMethod]
        public void DuplicateSectionRegistrationThrows()
        {
            _manager.Register(new FakeParticipant("mastery"));

            Assert.ThrowsException<ArgumentException>(() => _manager.Register(new FakeParticipant("mastery")));
        }

        [TestMethod]
        public void UnregisteredParticipantIsNotCaptured()
        {
            _manager.Register(new FakeParticipant("mastery"));
            _manager.Unregister("mastery");

            var file = _manager.Capture(new SaveMetadata());

            Assert.AreEqual(0, file.Sections.Count);
        }

        private static SaveFile FileWithSections(params string[] sectionIds)
        {
            var file = new SaveFile();
            foreach (string id in sectionIds)
                file.Sections[id] = new SaveSection { Version = 1, Data = new JObject() };
            return file;
        }

        private sealed class FakeParticipant(string sectionId, int version = 1, int restoreOrder = 0) : ISaveParticipant
        {
            public Func<JToken> OnCapture { get; set; } = () => new JObject();
            public Action<JToken>? OnRestore { get; set; }
            public Action<JToken, int>? OnRestoreWithVersion { get; set; }

            public string SectionId => sectionId;
            public int Version => version;
            public int RestoreOrder => restoreOrder;

            public JToken Capture() => OnCapture();

            public void Restore(JToken data, int savedVersion)
            {
                OnRestore?.Invoke(data);
                OnRestoreWithVersion?.Invoke(data, savedVersion);
            }
        }
    }
}

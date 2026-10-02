namespace LastBreathTest.Save
{
    using Battle.Source;
    using Core.Ai.World.Time;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Entity;
    using Core.Items.Grants;
    using Core.MessageBus;
    using Core.Save;
    using Core.Services;
    using Core.Session;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Newtonsoft.Json.Linq;

    [TestClass]
    public class SaveManagerTests
    {
        private const int Slot = 1;

        private string _saveRoot = null!;
        private LoadScope _loadScope = null!;
        private SaveManager _manager = null!;

        [TestInitialize]
        public void Setup()
        {
            _saveRoot = Path.Combine(Path.GetTempPath(), $"lastbreath_savemanager_{Guid.NewGuid():N}");
            _loadScope = new LoadScope();
            _manager = new SaveManager(_loadScope);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_saveRoot)) Directory.Delete(_saveRoot, recursive: true);
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
        public void ASectionOfAnUnregisteredParticipantDoesNotSurviveTheNextSave()
        {
            // A slot belongs to one project, so nothing in the file is "somebody else's" — a section
            // whose owner the build stopped composing has to leave with the save that dropped it,
            // instead of being copied out of the old file into every file that follows it.
            _manager.Register(new FakeParticipant("mastery"));
            _manager.Register(new FakeParticipant("raids"));
            Assert.IsTrue(_manager.Capture(new SaveMetadata()).Sections.ContainsKey("raids"), "the setup must actually write the section");

            _manager.Unregister("raids");
            var file = _manager.Capture(new SaveMetadata());

            Assert.IsFalse(file.Sections.ContainsKey("raids"), "the section outlived the participant that owned it");
            Assert.IsTrue(file.Sections.ContainsKey("mastery"));
        }

        [TestMethod]
        public void ARewrittenSlotLosesTheFieldsItsNewShapeDoesNotWrite()
        {
            // The save actually goes to disk and is read back, because the property belongs to the
            // file and not to one capture: a section is written whole, never patched field by field.
            // Bumping a section's format has to be able to retire a field for good — a save that
            // reached into the previous generation, at either seam, would leave the retired field in
            // the slot until the player deletes it, and hand it back on the next load.
            var storage = new SaveStorage(_saveRoot);
            _manager.Register(new FakeParticipant("abilityBook", version: 2)
            {
                OnCapture = () => new JObject { ["upgrades"] = new JObject { ["Ability_A"] = 1 }, ["retired"] = 7 }
            });
            storage.Write(Slot, _manager.Capture(new SaveMetadata())); // the slot as the older build left it
            Assert.IsNotNull(storage.Load(Slot)!.Sections["abilityBook"].Data["retired"], "the setup must actually write the old shape");

            _manager.Unregister("abilityBook");
            _manager.Register(new FakeParticipant("abilityBook", version: 3) { OnCapture = () => new JObject { ["sockets"] = new JObject() } });
            storage.Write(Slot, _manager.Capture(new SaveMetadata()));

            var section = storage.Load(Slot)!.Sections["abilityBook"];
            Assert.IsNull(section.Data["retired"], "the old shape's field survived the rewrite");
            Assert.IsNotNull(section.Data["sockets"]);
            Assert.AreEqual(3, section.Version);
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
        public void AMissingSectionRestoresFreshWithoutBeingReported()
        {
            bool restored = false;
            bool failed = false;
            var participant = new FakeParticipant("inventory") { OnRestore = _ => restored = true };
            _manager.Register(participant);
            _manager.SectionRestoreFailed += (_, _) => failed = true;

            _manager.Restore(new SaveFile()); // old save without the new section: not an error

            Assert.IsFalse(restored);
            Assert.AreEqual(1, participant.FreshRestores);
            Assert.IsFalse(failed);
        }

        [TestMethod]
        public void ASectionThatFailsToReadStillLeavesTheParticipantOnTheFreshState()
        {
            // "The section is damaged" must not land where "the section is absent" was already fixed
            // not to land: a participant nobody called at all. For a section owning scene state that
            // is a world the restore never populated.
            var failures = new List<string>();
            var broken = new FakeParticipant("spawnPoints") { OnRestore = _ => throw new InvalidOperationException("corrupt") };
            _manager.Register(broken);
            _manager.SectionRestoreFailed += (section, _) => failures.Add(section);

            _manager.Restore(FileWithSections("spawnPoints"));

            Assert.AreEqual(1, broken.FreshRestores, "the damaged section left the participant holding nothing");
            CollectionAssert.AreEqual(new[] { "spawnPoints" }, failures, "damage the player cannot see has to reach the log");
        }

        [TestMethod]
        public void ASectionThatReadsCleanlyIsNotAlsoGivenTheFreshState()
        {
            var participant = new FakeParticipant("mastery");
            _manager.Register(participant);

            _manager.Restore(FileWithSections("mastery"));

            Assert.AreEqual(0, participant.FreshRestores, "a healthy section is the participant's whole state");
        }

        [TestMethod]
        public void AFailingFreshRestoreIsReportedInsteadOfStoppingTheLoad()
        {
            var failures = new List<string>();
            bool secondRestored = false;
            _manager.Register(new FakeParticipant("broken", restoreOrder: 0)
            {
                OnRestoreWithoutSection = () => throw new InvalidOperationException("nothing to fall back on")
            });
            _manager.Register(new FakeParticipant("healthy", restoreOrder: 10) { OnRestore = _ => secondRestored = true });
            _manager.SectionRestoreFailed += (section, _) => failures.Add(section);

            _manager.Restore(FileWithSections("healthy"));

            Assert.IsTrue(secondRestored);
            CollectionAssert.AreEqual(new[] { "broken" }, failures);
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
        public void SectionNewerThanParticipantIsReportedAndRestoredFresh()
        {
            bool restored = false;
            var failures = new List<string>();
            var participant = new FakeParticipant("mastery", version: 1) { OnRestore = _ => restored = true };
            _manager.Register(participant);
            _manager.SectionRestoreFailed += (section, _) => failures.Add(section);

            var file = new SaveFile();
            file.Sections["mastery"] = new SaveSection { Version = 2, Data = new JObject() };
            _manager.Restore(file);

            Assert.IsFalse(restored, "data this build cannot read must not be handed to the participant");
            Assert.AreEqual(1, participant.FreshRestores);
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

        [TestMethod]
        public void RestoreStartsFromTheFreshSessionState()
        {
            var order = new List<string>();
            bool loadingDuringSection = false;
            var manager = new SaveManager(_loadScope, () => SessionResetting(() => order.Add("reset")));
            manager.Register(new FakeParticipant("mastery")
            {
                OnRestore = _ =>
                {
                    order.Add("mastery");
                    loadingDuringSection = _loadScope.IsLoading;
                }
            });

            manager.Restore(FileWithSections("mastery"));

            CollectionAssert.AreEqual(new[] { "reset", "mastery" }, order,
                "the sections are deltas on the fresh session, so the reset has to come first");
            Assert.IsTrue(loadingDuringSection, "the reset's own load scope closed the restore's");
        }

        [TestMethod]
        public void AFileFromTheFutureIsRejectedBeforeTheSessionIsTouched()
        {
            bool reset = false;
            var manager = new SaveManager(_loadScope, () => SessionResetting(() => reset = true));

            Assert.ThrowsException<InvalidOperationException>(() =>
                manager.Restore(new SaveFile { FormatVersion = SaveFile.CurrentFormatVersion + 1 }));

            Assert.IsFalse(reset, "a file the game refuses to read must not wipe the running playthrough");
        }

        [TestMethod]
        public void AMissingSectionLandsOnTheFreshValueNotOnThePreviousFile()
        {
            // Mastery stands in for every service that holds its own state and outlives the scene.
            // Without the reset the level of the file loaded before this one stays on the character —
            // and the next save writes it into the file that never carried it.
            var mastery = new MartialArtMastery(Mock.Of<IGameMessageBus>());
            ISaveManager manager = SaveStackWith(mastery);
            mastery.AddExperience(10_000);
            Assert.IsTrue(mastery.CurrentLevel > 0, "the setup must actually level up");

            manager.Restore(new SaveFile()); // a file written before the mastery section existed

            Assert.AreEqual(0, mastery.CurrentLevel);
            Assert.AreEqual(0, mastery.CurrentExperience);
        }

        /// <summary>A reset service holding one participant, standing in for the whole list.</summary>
        private ISessionResetService SessionResetting(Action onReset)
        {
            var service = new SessionResetService(_loadScope);
            var participant = new Mock<ISessionResettable>();
            participant.Setup(resettable => resettable.ResetSession()).Callback(onReset);
            service.Register(participant.Object);
            return service;
        }

        /// <summary>The save stack and the session reset exactly as a project composes them: no
        /// stand-in for the wiring that has to hand the manager its reset service.</summary>
        private static ISaveManager SaveStackWith(IMartialArtMastery mastery)
        {
            var services = new ServiceCollection();
            services.AddSingleton(mastery);
            services.AddSingleton(Mock.Of<IWorldClock>());
            services.AddSingleton(Mock.Of<IFactionRelationService>());
            services.AddSingleton(Mock.Of<IPlayerAccessor>());
            services.AddSingleton(Mock.Of<IAbilityProvider>());
            services.AddSingleton(Mock.Of<IGrantFactory>());
            services.AddSingleton(Mock.Of<ISpawnPointRegistry>(registry => registry.All == new List<IPersistentSpawnPoint>()));
            services.AddSaveSystem();
            services.AddSessionReset();

            return services.BuildServiceProvider().GetRequiredService<ISaveManager>();
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
            public Action? OnRestoreWithoutSection { get; set; }

            /// <summary>How many times the manager decided this file gives the participant nothing.</summary>
            public int FreshRestores { get; private set; }

            public string SectionId => sectionId;
            public int Version => version;
            public int RestoreOrder => restoreOrder;

            public JToken Capture() => OnCapture();

            public void Restore(JToken data, int savedVersion)
            {
                OnRestore?.Invoke(data);
                OnRestoreWithVersion?.Invoke(data, savedVersion);
            }

            public void RestoreWithoutSection()
            {
                FreshRestores++;
                OnRestoreWithoutSection?.Invoke();
            }
        }
    }
}

namespace LastBreathTest.WorldTesting
{
    using Core.Data.ChestData;
    using Core.Data.GameData;
    using Core.Enums;
    using Core.Inventory;
    using Core.Items;
    using Core.World.Containers;
    using Moq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    [TestClass]
    public class ChestContentsTests
    {
        private static IItem Item(string id)
        {
            var item = new Mock<IItem>();
            item.SetupGet(x => x.Id).Returns(id);
            return item.Object;
        }
        private static ChestDefinition Definition() => new("test", "test", 5, new(ChestAccessMode.Unlocked),
            new(ChestContentsMode.Authored, [new("first", "sword", 1, Rarity.Common), new("second", "ore", 10, Rarity.Common)]));

        private sealed class Receiver(Dictionary<string, int> capacity, Action? notify = null) : IInventoryTransfer
        {
            public readonly Dictionary<string, int> Accepted = [];
            public int ReceiveUpTo(IItem item, int amount)
            {
                int count = Math.Min(amount, capacity.GetValueOrDefault(item.Id));
                capacity[item.Id] = capacity.GetValueOrDefault(item.Id) - count;
                Accepted[item.Id] = Accepted.GetValueOrDefault(item.Id) + count;
                return count;
            }
            public IDisposable DeferNotifications() => new Scope(notify);
            private sealed class Scope(Action? action) : IDisposable { public void Dispose() => action?.Invoke(); }
        }

        [TestMethod]
        public void PartialStackAndLaterSlotsRemainAvailableAfterRefusal()
        {
            var chest = new ChestContents();
            chest.Initialize(Definition(), x => Item(x.ItemId));
            var result = chest.Transfer(new Receiver(new() { ["ore"] = 3 }), null, 100, 5, () => true);
            Assert.AreEqual(3, result.Accepted);
            Assert.IsTrue(result.CapacityLimited);
            Assert.AreEqual(1, chest.Slots[0].Amount);
            Assert.AreEqual(7, chest.Slots[1].Amount);
            Assert.AreEqual("second", chest.Slots[1].Id);
            Assert.IsNull(chest.RemoveAtMinutes);
        }

        [TestMethod]
        public void NotificationSeesCommittedSourceAndCannotReenterTransfer()
        {
            var chest = new ChestContents();
            chest.Initialize(Definition(), x => Item(x.ItemId));
            int observed = -1;
            Receiver? receiver = null;
            receiver = new Receiver(new() { ["sword"] = 1, ["ore"] = 10 }, () =>
            {
                observed = chest.Slots.Sum(x => x.Amount);
                Assert.AreEqual(105d, chest.RemoveAtMinutes);
                Assert.AreEqual(0, chest.Transfer(receiver!, null, 100, 5, () => true).Accepted);
            });
            chest.Transfer(receiver, null, 100, 5, () => true);
            Assert.AreEqual(0, observed);
            Assert.IsFalse(chest.RemovalDue(104.99));
            Assert.IsTrue(chest.RemovalDue(105));
            chest.Transfer(new Receiver(new()), null, 200, 5, () => true);
            Assert.AreEqual(105d, chest.RemoveAtMinutes);
        }

        [TestMethod]
        public void OpeningAndRestoringDoNotReroll()
        {
            var chest = new ChestContents();
            int calls = 0;
            chest.Initialize(Definition(), x => { calls++; return Item(x.ItemId); });
            var first = chest.Slots[0].Item;
            chest.Initialize(Definition(), _ => throw new Exception("Must not mint twice"));
            Assert.AreEqual(2, calls);
            Assert.AreSame(first, chest.Slots[0].Item);
            var restored = new ChestContents();
            restored.Restore(true, [new("first", first, 1), new("second", chest.Slots[1].Item, 7)], null);
            restored.Initialize(Definition(), _ => throw new Exception("Restore must not mint"));
            Assert.AreEqual(7, restored.Slots[1].Amount);
        }

        [TestMethod]
        public void FailedCreationDoesNotCommitPartialContents()
        {
            var chest = new ChestContents();
            Assert.ThrowsException<InvalidOperationException>(() => chest.Initialize(Definition(),
                x => x.ItemId == "ore" ? throw new InvalidOperationException() : Item(x.ItemId)));
            Assert.IsFalse(chest.Initialized);
            Assert.AreEqual(0, chest.Slots.Count);
        }

        [TestMethod]
        public void LostAccessAndUnknownSlotDoNotTransfer()
        {
            var chest = new ChestContents();
            chest.Initialize(Definition(), x => Item(x.ItemId));
            var receiver = new Receiver(new() { ["sword"] = 1, ["ore"] = 10 });
            Assert.AreEqual(0, chest.Transfer(receiver, null, 10, 5, () => false).Accepted);
            Assert.AreEqual(0, chest.Transfer(receiver, "absent", 10, 5, () => true).Accepted);
            Assert.AreEqual(11, chest.Slots.Sum(x => x.Amount));
        }

        [TestMethod]
        public void InvalidStateIsRejected()
        {
            Assert.ThrowsException<InvalidOperationException>(() => new ChestContents().Restore(true, [new("x", null, 1)], null));
            Assert.ThrowsException<InvalidOperationException>(() => new ChestContents().Restore(true, [], null));
        }
    }

    /// <summary>The catalog as the game reads it: the shipped file, and what a broken record costs.</summary>
    [TestClass]
    public class ChestCatalogTests
    {
        private const string StarterChest = "Chest_Starter";
        private const string TestFile = "chests-under-test.json";
        private const string ItemsPath = ChestFields.Contents + "." + ChestFields.Items;
        private const string FirstAmountPath = ItemsPath + "[0]." + ChestFields.Amount;

        /// <summary>The shipped chest, read through the real load path so the file's shape is held to
        /// what the participant expects of it.</summary>
        [TestMethod]
        public void ShippedStarterChestKeepsItsAuthoredContents()
        {
            ChestCatalog catalog = new();
            GameDataService service = new(new FileSystemDataSource(SharedData.Root()), [catalog]);
            List<string> failures = [];
            service.LoadFailed += (context, exception) => failures.Add($"{context}: {exception.Message}");

            service.LoadAll();

            Assert.AreEqual(0, failures.Count, string.Join(", ", failures));
            var starter = catalog.Find(StarterChest);
            Assert.IsNotNull(starter, "the shipped catalog no longer holds the starter chest");
            Assert.AreEqual(StarterChest, starter.NameKey);
            Assert.AreEqual(5d, starter.EmptyRemovalDelayMinutes);
            Assert.AreEqual(ChestAccessMode.Unlocked, starter.Access.Mode);
            Assert.AreEqual(ChestContentsMode.Authored, starter.Contents.Mode);
            Assert.AreEqual(5, starter.Contents.Items.Count);
            Assert.IsTrue(starter.Contents.Items.All(x => x.Rarity == Rarity.Uncommon && x.Amount == 1));
            Assert.IsTrue(starter.Contents.Items.Any(x => x.ItemId == "Boots_Stone_Tread"));
        }

        /// <summary>Every way a record is refused, written into one file beside two sound ones: the
        /// neighbours of a broken record still load, and the broken one is not half-read.</summary>
        [TestMethod]
        public void BrokenRecordsAreSkippedAndTheirNeighboursStillLoad()
        {
            ChestCatalog catalog = new();

            catalog.Apply(DataCatalog.Chests, new GameDataFile(TestFile, BrokenCatalogJson));

            Assert.AreEqual("Chest_Sound", catalog.Find("Chest_Sound")?.NameKey);
            Assert.AreEqual(2, catalog.Find("Chest_Sound")?.Contents.Items.Count);
            Assert.AreEqual("Chest_Last", catalog.Find("Chest_Last")?.NameKey);
            foreach (string skipped in (string[])
            [
                "Chest_UnknownAccessMode", "Chest_UnknownContentsMode", "Chest_RarityAsFlags",
                "Chest_NoDelay", "Chest_EmptyItemId", "Chest_RepeatedSlot", "Chest_NoPositions"
            ])
                Assert.IsNull(catalog.Find(skipped), skipped);
        }

        /// <summary>A value its field cannot hold — an object for text, text for a number or a section, a
        /// fraction for a count — costs its own record only: the sound records around it still load.</summary>
        [TestMethod]
        public void ValueOfTheWrongTypeCostsOnlyItsRecord()
        {
            ChestCatalog catalog = new();

            catalog.Apply(DataCatalog.Chests, new GameDataFile(TestFile, MistypedCatalogJson));

            Assert.AreEqual(2, catalog.Find("Chest_First")?.Contents.Items.Count);
            Assert.AreEqual(2, catalog.Find("Chest_Last")?.Contents.Items.Count);
            foreach (string skipped in (string[])
            [
                "Chest_NameKeyAsObject", "Chest_AccessAsText", "Chest_ItemsAsObject",
                "Chest_AmountAsText", "Chest_FractionalAmount", "Chest_DelayAsText"
            ])
                Assert.IsNull(catalog.Find(skipped), skipped);
        }

        /// <summary>An id the catalog does not hold is answered with nothing rather than a throw.</summary>
        [TestMethod]
        public void FindAnswersNothingForAnIdTheCatalogDoesNotHold()
        {
            ChestCatalog catalog = new();

            catalog.Apply(DataCatalog.Chests, new GameDataFile(TestFile, JsonConvert.SerializeObject(new[] { Sound("Chest_Known") })));

            Assert.AreEqual("Chest_Known", catalog.Find("Chest_Known")?.Id);
            Assert.IsNull(catalog.Find("Chest_Unknown"));
            Assert.IsNull(catalog.Find(string.Empty));
        }

        /// <summary>A second record under a taken id is refused, and the one already read is left alone.</summary>
        [TestMethod]
        public void RepeatedIdKeepsTheDefinitionAlreadyRead()
        {
            ChestCatalog catalog = new();

            catalog.Apply(DataCatalog.Chests, new GameDataFile(TestFile, BrokenCatalogJson));

            Assert.AreEqual("Chest_Sound", catalog.Find("Chest_Sound")?.NameKey);
            Assert.AreEqual("first", catalog.Find("Chest_Sound")?.Contents.Items[0].SlotId);
        }

        /// <summary>A file the reader cannot get records out of at all is refused whole, which is what
        /// hands it to the load service to report and skip.</summary>
        [TestMethod]
        public void UnreadableFileIsRefusedWholesale()
        {
            ChestCatalog catalog = new();

            Assert.ThrowsException<InvalidOperationException>(
                () => catalog.Apply(DataCatalog.Chests, new GameDataFile(TestFile, "null")));
            Assert.IsTrue(Refused(() => catalog.Apply(DataCatalog.Chests, new GameDataFile(TestFile, "{ not json"))));
            Assert.IsTrue(Refused(() => catalog.Apply(DataCatalog.Chests, new GameDataFile(TestFile, "{}"))));
        }

        private static bool Refused(Action apply)
        {
            try
            {
                apply();
                return false;
            }
            catch (Exception)
            {
                return true;
            }
        }

        private static object Position(string slotId, string itemId = "Weapon_Simple_Dagger", int amount = 1,
            string rarity = "Uncommon") => new { slotId, itemId, amount, rarity };

        private static object Contents(string mode, params object[] items) => new { mode, items };

        /// <summary>A record every rule accepts, with two positions.</summary>
        private static JObject Sound(string id) => JObject.FromObject(new
        {
            id, nameKey = id, emptyRemovalDelayMinutes = 5,
            access = new { mode = "Unlocked" },
            contents = Contents("Authored", Position("first"), Position("second"))
        });

        /// <summary>A sound record whose one field at <paramref name="path"/> holds <paramref name="value"/> instead.</summary>
        private static JObject Mistyped(string id, string path, JToken value)
        {
            var record = Sound(id);
            (record.SelectToken(path) ?? throw new ArgumentException($"a sound record has no '{path}'", nameof(path))).Replace(value);
            return record;
        }

        /// <summary>One file carrying every kind of value a field cannot hold between two sound records,
        /// plus a record whose id is not text and one that is not a record at all.</summary>
        private static string MistypedCatalogJson => JsonConvert.SerializeObject(new JToken[]
        {
            Sound("Chest_First"),
            Mistyped("Chest_NameKeyAsObject", ChestFields.NameKey, new JObject()),
            Mistyped("Chest_AccessAsText", ChestFields.Access, "Unlocked"),
            Mistyped("Chest_ItemsAsObject", ItemsPath, new JObject()),
            Mistyped("Chest_AmountAsText", FirstAmountPath, "many"),
            Mistyped("Chest_FractionalAmount", FirstAmountPath, 2.5),
            Mistyped("Chest_DelayAsText", ChestFields.Delay, "soon"),
            Mistyped("Chest_IdAsObject", ChestFields.Id, new JObject()),
            5,
            Sound("Chest_Last")
        });

        /// <summary>One file carrying every way a record is refused, sound records first, between and
        /// last. The records are composed rather than written out so that a broken one differs from a
        /// sound one in exactly the field it is named after.</summary>
        private static string BrokenCatalogJson => JsonConvert.SerializeObject(new object[]
        {
            new { id = "Chest_Sound", nameKey = "Chest_Sound", emptyRemovalDelayMinutes = 5,
                access = new { mode = "Unlocked" },
                contents = Contents("Authored", Position("first"), Position("second")) },
            new { id = "Chest_UnknownAccessMode", nameKey = "x", emptyRemovalDelayMinutes = 5,
                access = new { mode = "Picklocked" },
                contents = Contents("Authored", Position("first")) },
            new { id = "Chest_UnknownContentsMode", nameKey = "x", emptyRemovalDelayMinutes = 5,
                access = new { mode = "Unlocked" },
                contents = Contents("Random", Position("first")) },
            new { id = "Chest_RarityAsFlags", nameKey = "x", emptyRemovalDelayMinutes = 5,
                access = new { mode = "Unlocked" },
                contents = Contents("Authored", Position("first", rarity: "Uncommon, Common")) },
            new { id = "Chest_NoDelay", nameKey = "x",
                access = new { mode = "Unlocked" },
                contents = Contents("Authored", Position("first")) },
            new { id = "Chest_EmptyItemId", nameKey = "x", emptyRemovalDelayMinutes = 5,
                access = new { mode = "Unlocked" },
                contents = Contents("Authored", Position("first", itemId: "")) },
            new { id = "Chest_RepeatedSlot", nameKey = "x", emptyRemovalDelayMinutes = 5,
                access = new { mode = "Unlocked" },
                contents = Contents("Authored", Position("same"), Position("same", itemId: "Helmet_Stoneheart")) },
            new { id = "Chest_NoPositions", nameKey = "x", emptyRemovalDelayMinutes = 5,
                access = new { mode = "Unlocked" },
                contents = Contents("Authored") },
            new { id = "Chest_Sound", nameKey = "Chest_Impostor", emptyRemovalDelayMinutes = 5,
                access = new { mode = "Unlocked" },
                contents = Contents("Authored", Position("stolen", itemId: "Helmet_Stoneheart")) },
            new { id = "Chest_Last", nameKey = "Chest_Last", emptyRemovalDelayMinutes = 5,
                access = new { mode = "Unlocked" },
                contents = Contents("Authored", Position("first")) }
        });
    }
}

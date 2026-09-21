namespace LastBreathTest.WorldTesting
{
    using Core.Data.ChestData;
    using Core.Data.GameData;
    using Core.Enums;
    using Core.Inventory;
    using Core.Items;
    using Core.Modifiers;
    using Core.Services;
    using Core.World.Containers;
    using Moq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>Managed stand-ins for the items a chest holds: Moq proxies, no engine object behind them.</summary>
    internal static class ChestItems
    {
        /// <summary>The stack size of an item that does not stack.</summary>
        public const int Unstackable = 1;

        public static IItem Item(string id, int maxStackSize = Unstackable, Rarity rarity = Rarity.Common)
        {
            var item = new Mock<IItem>();
            item.SetupGet(x => x.Id).Returns(id);
            item.SetupGet(x => x.MaxStackSize).Returns(maxStackSize);
            item.SetupGet(x => x.Rarity).Returns(rarity);
            return item.Object;
        }
    }

    [TestClass]
    public class ChestContentsTests
    {
        private static List<ChestSlot> Slots() => [new("first", ChestItems.Item("sword"), 1), new("second", ChestItems.Item("ore"), 10)];

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
            chest.Initialize(Slots());
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
            chest.Initialize(Slots());
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

        /// <summary>Contents are taken once: initializing again, or after a restore, keeps what the chest already holds.</summary>
        [TestMethod]
        public void InitializingAgainChangesNothing()
        {
            var chest = new ChestContents();
            var first = Slots();
            chest.Initialize(first);

            chest.Initialize([new("other", ChestItems.Item("other"), 3)]);

            CollectionAssert.AreEqual(first, chest.Slots);
            var restored = new ChestContents();
            restored.Restore(true, [new("first", first[0].Item, 1), new("second", first[1].Item, 7)], null);
            restored.Initialize(Slots());
            Assert.AreEqual(7, restored.Slots[1].Amount);
            Assert.AreSame(first[1].Item, restored.Slots[1].Item);
        }

        [TestMethod]
        public void LostAccessAndUnknownSlotDoNotTransfer()
        {
            var chest = new ChestContents();
            chest.Initialize(Slots());
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

    /// <summary>Authored contents minted through a managed creation service: whole, or refused with no slot left behind.</summary>
    [TestClass]
    public class AuthoredChestMinterTests
    {
        private const string FailureMessage = "no item is written under this id";
        private const int OreStack = 20;
        private const int Pair = 2;
        private const int StarterPieces = 5;
        private const float NoBonusEffect = 0f;
        private const float BaseMultiplier = 1f;

        private static readonly AuthoredChestItem Sword = new("first", "sword", 1, Rarity.Common);
        private static readonly AuthoredChestItem Ore = new("second", "ore", 10, Rarity.Common);
        private static readonly AuthoredChestItem Gem = new("third", "gem", 1, Rarity.Rare);

        private static ChestDefinition Definition(params AuthoredChestItem[] positions) =>
            new("test", "test", 5, new(ChestAccessMode.Unlocked), new(ChestContentsMode.Authored, positions));

        private sealed record Request(string Id, int SignatureEffects, Rarity Rarity, float EquipEffectChance,
            float ModifierMultiplier, Rarity? FixedRarity);

        /// <summary>Creates a managed item for every request and remembers the requests in order: the ore stacks,
        /// everything else is a single piece, and the failing id throws instead.</summary>
        private sealed class Creation(string? failingId = null) : IItemCreationService
        {
            public List<Request> Requests { get; } = [];

            public string[] RequestedIds => Requests.Select(x => x.Id).ToArray();

            public IItem CreateItem(string id, List<string> additionalItemEffects, Rarity rarity, float equipEffectChance,
                float modifierMultiplier, Rarity? fixedRarity = null)
            {
                Requests.Add(new(id, additionalItemEffects.Count, rarity, equipEffectChance, modifierMultiplier, fixedRarity));
                if (id == failingId) throw new InvalidOperationException(FailureMessage);
                return ChestItems.Item(id, id == Ore.ItemId ? OreStack : ChestItems.Unstackable, rarity);
            }

            public IItem CreateItemByRecipe(string recipeId, IEnumerable<IModifierDescriptor> descriptors, Rarity? minRarity = null) =>
                throw new NotSupportedException("A chest mints by item id only.");
        }

        [TestMethod]
        public void EveryPositionIsMintedIntoItsSlotInAuthoredOrder()
        {
            var creation = new Creation();

            Assert.IsTrue(new AuthoredChestMinter(creation).TryMint(Definition(Sword, Ore, Gem), out var slots, out var refusal));

            Assert.IsNull(refusal);
            CollectionAssert.AreEqual(new[] { Sword.SlotId, Ore.SlotId, Gem.SlotId }, slots.Select(x => x.Id).ToArray());
            CollectionAssert.AreEqual(new[] { Sword.ItemId, Ore.ItemId, Gem.ItemId }, slots.Select(x => x.Item?.Id).ToArray());
            CollectionAssert.AreEqual(new[] { Sword.Amount, Ore.Amount, Gem.Amount }, slots.Select(x => x.Amount).ToArray());
            Assert.AreEqual(Gem.Rarity, slots[2].Item?.Rarity);
        }

        /// <summary>The shipped starter chest, read through the real load path, mints its five authored pieces at
        /// their authored rarity, with no bonus effect and at the base modifier scale.</summary>
        [TestMethod]
        public void ShippedStarterChestMintsItsFiveUncommonPieces()
        {
            ChestCatalog catalog = new();
            new GameDataService(new FileSystemDataSource(SharedData.Root()), [catalog]).LoadAll();
            var starter = catalog.Find(ChestCatalogTests.StarterChest);
            Assert.IsNotNull(starter, "the shipped catalog no longer holds the starter chest");
            var creation = new Creation();

            Assert.IsTrue(new AuthoredChestMinter(creation).TryMint(starter, out var slots, out _));

            Assert.AreEqual(StarterPieces, slots.Count);
            CollectionAssert.AreEqual(starter.Contents.Items.Select(x => x.SlotId).ToArray(), slots.Select(x => x.Id).ToArray());
            CollectionAssert.AreEqual(starter.Contents.Items.Select(x => x.ItemId).ToArray(), slots.Select(x => x.Item?.Id).ToArray());
            Assert.IsTrue(slots.All(x => x.Amount == 1 && x.Item?.Rarity == Rarity.Uncommon));
            Assert.IsTrue(creation.Requests.All(x => x.SignatureEffects == 0 && x.EquipEffectChance == NoBonusEffect
                && x.ModifierMultiplier == BaseMultiplier && x.FixedRarity == x.Rarity));
        }

        /// <summary>A position whose item cannot be created refuses the whole chest: no slot comes back, the failure
        /// is named, and the positions after it are never minted.</summary>
        [TestMethod]
        public void RefusalOnTheSecondPositionLeavesNoSlots()
        {
            var creation = new Creation(failingId: Ore.ItemId);

            Assert.IsFalse(new AuthoredChestMinter(creation).TryMint(Definition(Sword, Ore, Gem), out var slots, out var refusal));

            Assert.AreEqual(0, slots.Count);
            Assert.IsNotNull(refusal);
            Assert.AreEqual(Ore.SlotId, refusal.SlotId);
            Assert.AreEqual(Ore.ItemId, refusal.ItemId);
            StringAssert.Contains(refusal.Problem, FailureMessage);
            CollectionAssert.AreEqual(new[] { Sword.ItemId, Ore.ItemId }, creation.RequestedIds);
        }

        /// <summary>More than one of an item that does not stack is refused: the bag would put one instance into two slots.</summary>
        [TestMethod]
        public void UnstackableItemAskedForMoreThanOneIsRefused()
        {
            var boots = new AuthoredChestItem("boots", "Boots_Stone_Tread", Pair, Rarity.Uncommon);
            var creation = new Creation();

            Assert.IsFalse(new AuthoredChestMinter(creation).TryMint(Definition(Sword, boots, Gem), out var slots, out var refusal));

            Assert.AreEqual(0, slots.Count);
            Assert.IsNotNull(refusal);
            Assert.AreEqual(boots.SlotId, refusal.SlotId);
            Assert.AreEqual(boots.ItemId, refusal.ItemId);
            Assert.IsFalse(string.IsNullOrWhiteSpace(refusal.Problem));
            CollectionAssert.AreEqual(new[] { Sword.ItemId, boots.ItemId }, creation.RequestedIds);
        }
    }

    /// <summary>The catalog as the game reads it: the shipped file, and what a broken record costs.</summary>
    [TestClass]
    public class ChestCatalogTests
    {
        internal const string StarterChest = "Chest_Starter";
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

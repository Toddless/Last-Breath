namespace LastBreathTest.WorldTesting
{
    using System.Globalization;
    using Core.Data.ChestData;
    using Core.Data.GameData;
    using Core.Enums;
    using Core.Inventory;
    using Core.Items;
    using Core.Modifiers;
    using Core.Services;
    using Core.World.Containers;
    using LastBreath.Descriptors;
    using LootSimulation;
    using Moq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Tooling.Catalogs;

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

    /// <summary>A saved chest state made into one the chest can hold: content changed since the save is repaired and
    /// reported, and a state that cannot be read is refused whole.</summary>
    [TestClass]
    public class ChestStateRepairTests
    {
        private const string Placed = "Chest_Placed";
        private const string Renamed = "Chest_Renamed";
        private const string First = "first";
        private const string Second = "second";
        private const string SwordId = "sword";
        private const string OreId = "ore";
        private const string ReadProblem = "the record is not an object";
        private const int OreStack = 20;
        private const int OneLeft = 1;
        private const int OreLeft = 7;
        private const int NoneLeft = 0;
        private const double Now = 100d;
        private const double Delay = 5d;
        private const double SavedDeadline = 42d;

        private static readonly IItem Sword = ChestItems.Item(SwordId);
        private static readonly IItem Ore = ChestItems.Item(OreId, OreStack);

        private static SavedChestState Saved(bool initialized, double? removeAt, params ChestSlot[] slots) =>
            new(Placed, initialized, slots, removeAt);

        private static SavedChestState Opened(params ChestSlot[] slots) => Saved(true, null, slots);

        private static ChestStateRepairResult Repair(SavedChestState saved, double? delay = Delay) =>
            ChestStateRepair.Repair(saved, Placed, Now, delay);

        private static void AssertProblems(ChestStateRepairResult result, params ChestStateProblem[] expected) =>
            CollectionAssert.AreEqual(expected, result.Findings.Select(x => x.Problem).ToArray(),
                string.Join("; ", result.Findings.Select(x => x.Description)));

        /// <summary>The repaired state taken by a fresh chest the way the component takes it: a state the repair hands
        /// back never trips the invariants of ChestContents.</summary>
        private static ChestContents Restored(ChestStateRepairResult result)
        {
            Assert.IsNotNull(result.State, "the repair refused a state it should restore");
            ChestContents contents = new();
            contents.Restore(result.State.Initialized, result.State.Slots, result.State.RemoveAtMinutes);
            return contents;
        }

        /// <summary>A save under the same definition with every item read back comes back slot for slot, item for item.</summary>
        [TestMethod]
        public void SoundSaveComesBackAsSavedWithoutFindings()
        {
            var result = Repair(Opened(new(First, Sword, OneLeft), new(Second, Ore, OreLeft)));

            AssertProblems(result);
            var chest = Restored(result);
            Assert.IsTrue(chest.Initialized);
            Assert.IsNull(chest.RemoveAtMinutes);
            CollectionAssert.AreEqual(new[] { First, Second }, chest.Slots.Select(x => x.Id).ToArray());
            CollectionAssert.AreEqual(new[] { OneLeft, OreLeft }, chest.Slots.Select(x => x.Amount).ToArray());
            Assert.AreSame(Sword, chest.Slots[0].Item);
            Assert.AreSame(Ore, chest.Slots[1].Item);
        }

        /// <summary>An unopened chest stays unopened and an emptied one keeps the deadline it was saved with.</summary>
        [TestMethod]
        public void SoundUnopenedAndEmptiedChestsComeBackAsSaved()
        {
            var unopened = Repair(Saved(false, null));
            var emptied = Repair(Saved(true, SavedDeadline, new ChestSlot(First, null, NoneLeft)));

            AssertProblems(unopened);
            AssertProblems(emptied);
            Assert.IsFalse(Restored(unopened).Initialized);
            Assert.AreEqual(SavedDeadline, Restored(emptied).RemoveAtMinutes);
        }

        /// <summary>The concrete items of the save are the truth: a chest placed with another definition keeps them.</summary>
        [TestMethod]
        public void ChangedDefinitionIsReportedAndTheSavedSlotsAreKept()
        {
            var result = Repair(Opened(new ChestSlot(First, Sword, OneLeft)) with { DefinitionId = Renamed });

            AssertProblems(result, ChestStateProblem.DefinitionChanged);
            StringAssert.Contains(result.Findings[0].Description, Renamed);
            StringAssert.Contains(result.Findings[0].Description, Placed);
            Assert.AreSame(Sword, Restored(result).Slots.Single().Item);
        }

        /// <summary>An unopened chest under a changed definition is still unopened: it mints from the definition it is placed with.</summary>
        [TestMethod]
        public void UnopenedChestUnderAChangedDefinitionStaysUnopened()
        {
            var result = Repair(Saved(false, null) with { DefinitionId = Renamed });

            AssertProblems(result, ChestStateProblem.DefinitionChanged);
            Assert.IsFalse(Restored(result).Initialized);
        }

        /// <summary>A slot whose item could not be read back is emptied in place; the slots around it keep their items.</summary>
        [TestMethod]
        public void SlotWhoseItemIsMissingIsEmptiedAndTheOthersAreKept()
        {
            ChestSlot missing = new(First, null, OneLeft);

            var result = Repair(Opened(missing, new(Second, Ore, OreLeft)));

            AssertProblems(result, ChestStateProblem.ItemMissing);
            StringAssert.Contains(result.Findings[0].Description, First);
            var chest = Restored(result);
            CollectionAssert.AreEqual(new[] { First, Second }, chest.Slots.Select(x => x.Id).ToArray());
            Assert.AreEqual(NoneLeft, chest.Slots[0].Amount);
            Assert.AreEqual(OreLeft, chest.Slots[1].Amount);
            Assert.AreSame(Ore, chest.Slots[1].Item);
            Assert.IsNull(chest.RemoveAtMinutes, "a chest still holding ore started its removal");
            Assert.AreEqual(OneLeft, missing.Amount, "the repair changed the slot it was handed");
        }

        /// <summary>A chest left with nothing once its missing items are emptied starts the placed definition's delay now.</summary>
        [TestMethod]
        public void ChestLeftWithNothingGetsTheDefinitionDelay()
        {
            var result = Repair(Opened(new(First, null, OneLeft), new(Second, null, OreLeft)));

            AssertProblems(result, ChestStateProblem.ItemMissing, ChestStateProblem.ItemMissing, ChestStateProblem.DeadlineMissing);
            var chest = Restored(result);
            Assert.AreEqual(Now + Delay, chest.RemoveAtMinutes);
            Assert.IsFalse(chest.RemovalDue(Now));
            Assert.IsTrue(chest.RemovalDue(Now + Delay));
        }

        /// <summary>Without a definition there is no delay to wait out: the emptied chest is due for removal at once.</summary>
        [TestMethod]
        public void EmptiedChestWithoutDeadlineIsDueAtOnceWhenItsDefinitionIsUnknown()
        {
            var result = Repair(Opened(new ChestSlot(First, null, NoneLeft)), delay: null);

            AssertProblems(result, ChestStateProblem.DeadlineMissing);
            var chest = Restored(result);
            Assert.AreEqual(Now, chest.RemoveAtMinutes);
            Assert.IsTrue(chest.RemovalDue(Now));
        }

        /// <summary>A state from a newer build is refused with nothing to restore, so nothing is minted in its place.</summary>
        [TestMethod]
        public void VersionThisBuildDoesNotReadIsRefused()
        {
            int newer = ChestStateRepair.StateVersion + 1;

            var result = ChestStateRepair.VersionNotRead(newer);

            Assert.IsTrue(ChestStateRepair.Reads(ChestStateRepair.StateVersion));
            Assert.IsFalse(ChestStateRepair.Reads(newer));
            Assert.IsNull(result.State);
            AssertProblems(result, ChestStateProblem.VersionNotRead);
            StringAssert.Contains(result.Findings[0].Description, newer.ToString(CultureInfo.InvariantCulture));
        }

        [TestMethod]
        public void UnreadableStateIsRefusedWithItsProblem()
        {
            var result = ChestStateRepair.Unreadable(ReadProblem);

            Assert.IsNull(result.State);
            AssertProblems(result, ChestStateProblem.Unreadable);
            StringAssert.Contains(result.Findings[0].Description, ReadProblem);
        }

        /// <summary>A state the repairs cannot make coherent is refused whole with one finding: none of the repairs it
        /// would have needed is reported, and nothing reaches ChestContents to throw.</summary>
        [TestMethod]
        public void IncoherentStateIsRefusedWholeRatherThanRepaired()
        {
            var repeatedSlot = Repair(Opened(new(First, Sword, OneLeft), new(First, null, OneLeft)) with { DefinitionId = Renamed });
            var unopenedWithContents = Repair(Saved(false, null, new ChestSlot(First, Sword, OneLeft)));
            var deadlineOverContents = Repair(Saved(true, SavedDeadline, new ChestSlot(First, Sword, OneLeft)));

            foreach (var (name, result) in new[]
                     {
                         (nameof(repeatedSlot), repeatedSlot), (nameof(unopenedWithContents), unopenedWithContents),
                         (nameof(deadlineOverContents), deadlineOverContents)
                     })
            {
                Assert.IsNull(result.State, name);
                AssertProblems(result, ChestStateProblem.Unreadable);
            }
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

        /// <summary>Seeds the real creation service: the rolls decide what a piece rolls, never whether its id mints.</summary>
        private const int AuditSeed = 20260710;

        private const string FaultFormat = "{0} / {1} / {2}: {3}";
        private const string NoRecordsFault = "the shipped chest file writes no records";
        private const string UnnamedRecordFormat = "record {0} of the shipped chest file writes no id the catalog could hold";
        private const string NotHeldFormat = "{0}: the catalog holds no chest under this id, so the record was skipped as broken";
        private const string SlotCountFormat = "{0}: {1} slots were minted for {2} authored positions";
        private const string SlotIdFormat = "minted into slot '{0}'";
        private const string ItemIdFormat = "minted item '{0}'";
        private const string AmountFormat = "minted {0} of it";
        private const string RarityFormat = "minted at {0}";

        private static readonly AuthoredChestItem Sword = new("first", "sword", 1, Rarity.Common);
        private static readonly AuthoredChestItem Ore = new("second", "ore", 10, Rarity.Common);
        private static readonly AuthoredChestItem Gem = new("third", "gem", 1, Rarity.Rare);

        /// <summary>A chest just big enough for its positions, as the catalog would believe one.</summary>
        private static ChestDefinition Definition(params AuthoredChestItem[] positions) =>
            new("test", "test", positions.Length, 5, new(ChestAccessMode.Unlocked), new(ChestContentsMode.Authored, positions));

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

        /// <summary>Every chest the shipped file writes, found by the id read straight off the json, is held by the catalog
        /// and mints whole through the real creation service, each slot as authored: a mistyped item id, or a record the
        /// catalog skips as broken, fails here rather than in the game.</summary>
        [TestMethod]
        public void EveryShippedChestMintsWholeFromTheRealItemData()
        {
            var faults = ChestFaults(SharedData.Root(), LootPipeline.Create(AuditSeed).ItemCreation);

            Assert.AreEqual(0, faults.Count, string.Join(Environment.NewLine, faults));
        }

        /// <summary>Everything that keeps the chests written under <paramref name="dataRoot"/> from minting as authored,
        /// one line per fault.</summary>
        private static List<string> ChestFaults(string dataRoot, IItemCreationService creation)
        {
            ChestCatalog catalog = new();
            new GameDataService(new FileSystemDataSource(dataRoot), [catalog]).LoadAll();
            var minter = new AuthoredChestMinter(creation);
            var ids = WrittenChestIds(dataRoot);

            return ids.Count == 0 ? [NoRecordsFault] : [.. ids.SelectMany((id, index) => RecordFaults(id, index, catalog, minter))];
        }

        /// <summary>The id each record of the chest file writes, read off the json rather than the catalog; null where a
        /// record writes none as text.</summary>
        private static List<string?> WrittenChestIds(string dataRoot)
        {
            string file = Path.Combine(dataRoot, DataCatalog.Chests, ChestsCatalogDescriptor.FileName + CatalogWorkspace.FileExtension);
            return [.. JArray.Parse(File.ReadAllText(file)).Select(IdOf)];
        }

        private static string? IdOf(JToken record) =>
            record is JObject fields && fields[ChestFields.Id] is JValue { Value: string id } && !string.IsNullOrWhiteSpace(id) ? id : null;

        /// <summary>The faults of one written record: no id, no chest in the catalog under it, or what its mint says.</summary>
        private static IEnumerable<string> RecordFaults(string? id, int index, ChestCatalog catalog, AuthoredChestMinter minter)
        {
            if (id == null) return [string.Format(CultureInfo.InvariantCulture, UnnamedRecordFormat, index)];
            return catalog.Find(id) is { } chest ? MintFaults(minter, chest) : [string.Format(CultureInfo.InvariantCulture, NotHeldFormat, id)];
        }

        /// <summary>The refusal of a chest that does not mint whole, or every slot minted other than its authored position.</summary>
        private static IEnumerable<string> MintFaults(AuthoredChestMinter minter, ChestDefinition chest)
        {
            if (!minter.TryMint(chest, out var slots, out var refusal))
                return [Fault(chest.Id, refusal.SlotId, refusal.ItemId, refusal.Problem)];
            if (slots.Count != chest.Contents.Items.Count)
                return [string.Format(CultureInfo.InvariantCulture, SlotCountFormat, chest.Id, slots.Count, chest.Contents.Items.Count)];

            return chest.Contents.Items.Zip(slots).SelectMany(pair =>
                SlotDifferences(pair.First, pair.Second).Select(problem => Fault(chest.Id, pair.First.SlotId, pair.First.ItemId, problem)));
        }

        /// <summary>What a minted slot says other than its position: the slot, the item, the amount, and the rarity equipment
        /// is minted at.</summary>
        private static IEnumerable<string> SlotDifferences(AuthoredChestItem position, ChestSlot slot)
        {
            if (slot.Id != position.SlotId) yield return string.Format(CultureInfo.InvariantCulture, SlotIdFormat, slot.Id);
            if (slot.Item?.Id != position.ItemId) yield return string.Format(CultureInfo.InvariantCulture, ItemIdFormat, slot.Item?.Id);
            if (slot.Amount != position.Amount) yield return string.Format(CultureInfo.InvariantCulture, AmountFormat, slot.Amount);
            if (slot.Item is IEquipItem equip && equip.Rarity != position.Rarity)
                yield return string.Format(CultureInfo.InvariantCulture, RarityFormat, equip.Rarity);
        }

        /// <summary>One fault line: the chest, the slot and the item it belongs to, and what went wrong.</summary>
        private static string Fault(string chest, string slot, string item, string problem) =>
            string.Format(CultureInfo.InvariantCulture, FaultFormat, chest, slot, item, problem);

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

        /// <summary>The cells the shipped starter chest is authored with.</summary>
        private const int StarterCells = 20;

        /// <summary>The cells of every record composed here: more than any of them has positions.</summary>
        private const int Cells = 10;

        /// <summary>The positions a sound record writes.</summary>
        private const int SoundPositions = 2;

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
            Assert.AreEqual(StarterCells, starter.Capacity);
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

        /// <summary>A capacity left out, not a whole number, none, less than none, or fewer cells than the
        /// positions costs its own record only; positions filling every cell are sound.</summary>
        [TestMethod]
        public void CapacityThatCannotHoldThePositionsCostsOnlyItsRecord()
        {
            ChestCatalog catalog = new();

            catalog.Apply(DataCatalog.Chests, new GameDataFile(TestFile, CapacityCatalogJson));

            Assert.AreEqual(Cells, catalog.Find("Chest_First")?.Capacity);
            Assert.AreEqual(Cells, catalog.Find("Chest_Last")?.Capacity);
            var filled = catalog.Find("Chest_EveryCellFilled");
            Assert.AreEqual(SoundPositions, filled?.Capacity);
            Assert.AreEqual(SoundPositions, filled?.Contents.Items.Count);
            foreach (string skipped in (string[])
            [
                "Chest_NoCapacity", "Chest_CapacityAsText", "Chest_FractionalCapacity",
                "Chest_ZeroCapacity", "Chest_NegativeCapacity", "Chest_MorePositionsThanCells"
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
            id, nameKey = id, capacity = Cells, emptyRemovalDelayMinutes = 5,
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

        /// <summary>A sound record with its field <paramref name="field"/> left out.</summary>
        private static JObject Without(string id, string field)
        {
            var record = Sound(id);
            if (!record.Remove(field)) throw new ArgumentException($"a sound record has no '{field}'", nameof(field));
            return record;
        }

        /// <summary>Two sound records around every capacity the reader refuses, and one whose positions fill every
        /// cell. Each record is composed from a sound one and differs from it in the capacity alone.</summary>
        private static string CapacityCatalogJson => JsonConvert.SerializeObject(new JToken[]
        {
            Sound("Chest_First"),
            Without("Chest_NoCapacity", ChestFields.Capacity),
            Mistyped("Chest_CapacityAsText", ChestFields.Capacity, "many"),
            Mistyped("Chest_FractionalCapacity", ChestFields.Capacity, 2.5),
            Mistyped("Chest_ZeroCapacity", ChestFields.Capacity, 0),
            Mistyped("Chest_NegativeCapacity", ChestFields.Capacity, -1),
            Mistyped("Chest_MorePositionsThanCells", ChestFields.Capacity, SoundPositions - 1),
            Mistyped("Chest_EveryCellFilled", ChestFields.Capacity, SoundPositions),
            Sound("Chest_Last")
        });

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
            new { id = "Chest_Sound", nameKey = "Chest_Sound", capacity = Cells, emptyRemovalDelayMinutes = 5,
                access = new { mode = "Unlocked" },
                contents = Contents("Authored", Position("first"), Position("second")) },
            new { id = "Chest_UnknownAccessMode", nameKey = "x", capacity = Cells, emptyRemovalDelayMinutes = 5,
                access = new { mode = "Picklocked" },
                contents = Contents("Authored", Position("first")) },
            new { id = "Chest_UnknownContentsMode", nameKey = "x", capacity = Cells, emptyRemovalDelayMinutes = 5,
                access = new { mode = "Unlocked" },
                contents = Contents("Random", Position("first")) },
            new { id = "Chest_RarityAsFlags", nameKey = "x", capacity = Cells, emptyRemovalDelayMinutes = 5,
                access = new { mode = "Unlocked" },
                contents = Contents("Authored", Position("first", rarity: "Uncommon, Common")) },
            new { id = "Chest_NoDelay", nameKey = "x", capacity = Cells,
                access = new { mode = "Unlocked" },
                contents = Contents("Authored", Position("first")) },
            new { id = "Chest_EmptyItemId", nameKey = "x", capacity = Cells, emptyRemovalDelayMinutes = 5,
                access = new { mode = "Unlocked" },
                contents = Contents("Authored", Position("first", itemId: "")) },
            new { id = "Chest_RepeatedSlot", nameKey = "x", capacity = Cells, emptyRemovalDelayMinutes = 5,
                access = new { mode = "Unlocked" },
                contents = Contents("Authored", Position("same"), Position("same", itemId: "Helmet_Stoneheart")) },
            new { id = "Chest_NoPositions", nameKey = "x", capacity = Cells, emptyRemovalDelayMinutes = 5,
                access = new { mode = "Unlocked" },
                contents = Contents("Authored") },
            new { id = "Chest_Sound", nameKey = "Chest_Impostor", capacity = Cells, emptyRemovalDelayMinutes = 5,
                access = new { mode = "Unlocked" },
                contents = Contents("Authored", Position("stolen", itemId: "Helmet_Stoneheart")) },
            new { id = "Chest_Last", nameKey = "Chest_Last", capacity = Cells, emptyRemovalDelayMinutes = 5,
                access = new { mode = "Unlocked" },
                contents = Contents("Authored", Position("first")) }
        });
    }
}

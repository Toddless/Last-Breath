namespace LastBreathTest.BattleSystemTests
{
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Data;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Items;
    using Core.Items.Grants;
    using Core.Save;
    using Core.Save.Participants;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// An augment as a thing that can be owned. Until it was one, an augment could be described,
    /// judged and seated but never come by: nothing dropped it, nothing held it, nothing remembered
    /// it — the sockets a character opens stood empty because the game had no way to hand him a
    /// single augment to put in them.
    ///
    /// What being a thing costs is walked here. It has to be minted as one; it has to lie in the bag
    /// as ITSELF, because a copy is its numbers and two copies of one record are two different
    /// augments; it has to come back out of a save file with those numbers, which nothing in the game
    /// can draw a second time; and an id no record declares has to mint nothing at all, since the one
    /// door every id goes through offers it to every kind there is.
    /// </summary>
    [TestClass]
    public class AugmentItemTests
    {
        /// <summary>A shipped record whose number is a share, so two draws around it are two values
        /// rather than one rounded count.</summary>
        private const string Record = "Augment_Reduce_Cost";

        private const string Property = "costShare";

        private const string NoSuchRecord = "Augment_No_File_Declares";

        /// <summary>The band the draws below are taken over. Pinned here rather than read off the
        /// shipped rules: a balance pass closing the band would otherwise make "two copies differ"
        /// a claim about the file instead of about the augment.</summary>
        private const float Spread = 0.25f;

        private const int BagSlots = 8;

        [TestMethod]
        public void AMintedAugmentIsAThingTheBagHolds()
        {
            // The whole point of the item: a minted augment goes into the bag and is reachable there
            // as an augment — not as an id the bag happens to be carrying.
            var bag = new SlottedBag(BagSlots);
            IAugmentItem? minted = Minter(seed: 31).Mint(Record);
            Assert.IsNotNull(minted, $"the shipped data declares no '{Record}'");

            Assert.IsTrue(bag.TryAddItem(minted));

            var held = bag.GetItem<IAugmentItem>(minted.InstanceId);
            Assert.IsNotNull(held, "the bag holds the augment as something other than an augment");
            Assert.AreEqual(Record, held.Id, "the item does not name the record it was made from");
            Assert.AreEqual(minted.Augment.Values[Property], held.Augment.Values[Property],
                "the bag handed back an augment with numbers other than the ones minted");
            Assert.AreEqual(1, bag.OccupiedSlots.Count);
        }

        [TestMethod]
        public void TwoCopiesOfOneRecordLieInTheBagSeparately()
        {
            // Two copies of one record are not the same augment — one rolled higher than the other —
            // so a bag that merged them by id would leave the player holding "two" of a thing that
            // can only be one of them, and one of the two draws would be gone.
            var bag = new SlottedBag(BagSlots);
            AugmentItemMinter minter = Minter(seed: 7);
            IAugmentItem first = minter.Mint(Record)!;
            IAugmentItem second = minter.Mint(Record)!;
            Assert.AreNotEqual(first.Augment.Values[Property], second.Augment.Values[Property],
                "the two draws came out identical, so nothing below could tell a merge from a pair");

            bag.TryAddItem(first);
            bag.TryAddItem(second);

            Assert.AreEqual(2, bag.OccupiedSlots.Count, "the two copies were merged into one slot");
            Assert.AreEqual(2, bag.GetContents().Count, "the bag holds one entry where the player has two augments");
            Assert.AreEqual(first.Augment.Values[Property], bag.GetItem<IAugmentItem>(first.InstanceId)!.Augment.Values[Property]);
            Assert.AreEqual(second.Augment.Values[Property], bag.GetItem<IAugmentItem>(second.InstanceId)!.Augment.Values[Property]);
        }

        [TestMethod]
        public void TheBagWalkedHereStillMergesWhatIsMeantToStack()
        {
            // The control the case above needs: the bag CAN put two arrivals of one id into a single
            // slot, so the augments staying apart is the augment's doing and not the bag's inability.
            var bag = new SlottedBag(BagSlots);

            bag.TryAddItem(Stackable("Crafting_Resource_Coal"), 4);
            bag.TryAddItem(Stackable("Crafting_Resource_Coal"), 3);

            Assert.AreEqual(1, bag.OccupiedSlots.Count, "the bag never merges anything, so separateness proves nothing");
            Assert.AreEqual(7, bag.GetTotalItemAmount("Crafting_Resource_Coal"));
        }

        [TestMethod]
        public void ACopyInTheBagComesBackFromTheFileWithItsOwnNumbers()
        {
            // The numbers are the copy, they were drawn once and nothing in the game can draw them
            // again — a load that rebuilt the augment from its record alone would hand the player a
            // different augment than the one he put away.
            var bag = new SlottedBag(BagSlots);
            AugmentItemMinter minter = Minter(seed: 12);
            IAugmentItem stored = minter.Mint(Record)!;
            bag.TryAddItem(stored);
            var participant = Participant(bag, minter);

            JToken written = JToken.Parse(participant.Capture().ToString(Formatting.None)); // through the file, not around it
            bag.TryAddItem(minter.Mint(Record)!); // the load must land on what the file says, not on top of it
            participant.Restore(written, participant.Version);

            var contents = bag.GetContents();
            Assert.AreEqual(1, contents.Count, "the restored bag is not the one the file described");
            var restored = contents[0].Item as IAugmentItem;
            Assert.IsNotNull(restored, "what came back out of the file is not an augment");
            Assert.AreEqual(stored.Augment.AugmentId, restored.Augment.AugmentId);
            Assert.AreEqual(stored.Augment.Values[Property], restored.Augment.Values[Property],
                "the copy came back at numbers other than the ones it rolled");
            Assert.AreEqual(stored.Rarity, restored.Rarity);
        }

        [TestMethod]
        public void AnIdNoRecordDeclaresMintsNoAugment()
        {
            // Nothing says what the augment is or what to draw around. An item minted anyway would be
            // an augment that parses, seats and does nothing at all.
            Assert.IsNull(Minter(seed: 1).Mint(NoSuchRecord));
        }

        [TestMethod]
        public void AnAugmentIdIsClaimedByItsOwnKindAtTheDoorEveryIdGoesThrough()
        {
            // The shared door offers an id to every kind of thing there is and copies a plain template
            // only when none claims it. An augment id that reached that copy would be looked for among
            // the resources, where no augment is written.
            var items = new Mock<IItemDataProvider>(MockBehavior.Strict);
            items.Setup(provider => provider.GetBlueprint(It.IsAny<string>())).Returns((EquipItemBlueprint?)null);
            var door = new ItemMinter(items.Object, Mock.Of<IEquipItemMinter>(), items.Object, Minter(seed: 4));

            IItem minted = door.MintItem(Record);

            Assert.IsTrue(minted is IAugmentItem, "the augment id found no kind of its own and fell through to a plain copy");
        }

        [TestMethod]
        public void AnIdOfNoKindStillFallsThroughToThePlainCopy()
        {
            // The other side of the door: the kinds ask, they do not swallow. An id no kind claims —
            // an augment-shaped one included — is still the resource it always was.
            var copy = Mock.Of<IItem>(resource => resource.Id == NoSuchRecord);
            var items = new Mock<IItemDataProvider>();
            items.Setup(provider => provider.GetBlueprint(It.IsAny<string>())).Returns((EquipItemBlueprint?)null);
            items.Setup(provider => provider.CopyItem(NoSuchRecord)).Returns(copy);
            var door = new ItemMinter(items.Object, Mock.Of<IEquipItemMinter>(), items.Object, Minter(seed: 4));

            Assert.AreSame(copy, door.MintItem(NoSuchRecord));
        }

        [TestMethod]
        public void ACompositionThatMintsNoAugmentsStillBuildsTheDoor()
        {
            // The augment kind needs the band its numbers are drawn around, and that is registered by
            // the module that fights. A sandbox composed without it must still get its item minter —
            // the seam is optional, and a required dependency here would take the door down in every
            // project that never mints an augment.
            var services = new ServiceCollection();
            services.AddSingleton(Mock.Of<IItemDataProvider>());
            services.AddSingleton(provider => provider.GetRequiredService<IItemDataProvider>() as IEquipBlueprintProvider);
            services.AddSingleton(Mock.Of<IEquipItemMinter>());
            services.AddSingleton<IItemMinter, ItemMinter>();

            using var provider = services.BuildServiceProvider();

            Assert.IsNotNull(provider.GetService<IItemMinter>(), "a composition without augments cannot build its item minter at all");
        }

        /// <summary>The bag section as the game registers it, minus the item data: an augment must
        /// travel as an augment, and a strict provider fails the walk the moment one is written down
        /// or read back as a resource id.</summary>
        private static InventorySaveParticipant Participant(SlottedBag bag, IAugmentItemMinter augments) =>
            new(bag, new Mock<IItemDataProvider>(MockBehavior.Strict).Object,
                new EquipItemSaveConverter(new GrantFactory(() => null, () => null, () => null)), augments);

        /// <summary>The minter the game composes, over the shipped records and a band of a width the
        /// case names.</summary>
        private static AugmentItemMinter Minter(int seed)
        {
            AbilityAugmentCatalog catalog = ShippedAbilityData.Augments();
            return new AugmentItemMinter(catalog, new AugmentMinter(catalog,
                new StubCombatRules(new AugmentValueRules(Spread)), new DefaultRandomNumberGenerator(seed)));
        }

        /// <summary>A plain template of the kind the bag is built to pile up.</summary>
        private static CraftingResource Stackable(string id) => new(id, 20, [], null!, Rarity.Common);
    }
}

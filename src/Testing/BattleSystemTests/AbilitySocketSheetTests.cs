namespace LastBreathTest.BattleSystemTests
{
    using Battle.Source.Abilities;
    using Battle.Source.RequestHandlers;
    using Battle.Source.UIElements.PassiveWheel;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Data;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Inventory;
    using Core.Items;
    using Core.Localization;
    using Core.MessageBus.Requests;
    using Core.PassiveTree;
    using Core.Services;
    using Core.Views;
    using Core.Views.UI;
    using Moq;
    using static AbilityBookStand;
    using static AugmentCopies;

    /// <summary>
    /// What the socket screen is handed. The sheet is a picture of the BOARD and of the book, and of
    /// nothing else: a row exists for an ability the player owns or for one still holding his augments,
    /// an ability he has never unlocked is not on it at all, and a row carries exactly the slots the
    /// allocation opened — empty ones included, because an empty slot is the thing he drags an augment
    /// onto.
    ///
    /// The refusal a window shows comes from the gate's own answer. A second table of reasons beside
    /// the rule would be a second reading of it, free to offer what the board then refuses.
    /// </summary>
    [TestClass]
    public class AbilitySocketSheetTests
    {
        private const string OwnedAbility = "Ability_Owned";
        private const string OtherOwnedAbility = "Ability_Owned_Two";
        private const string NeverUnlocked = "Ability_Never_Unlocked";
        private const string StrengthAbility = "Ability_Strength";

        private const string UnlockNode = "abilityunlock_owned";
        private const string SocketNode = "sockettier2_owned";

        private const string Augment = "Augment_Sharpened";

        /// <summary>What the stub's abilities cost, so a row with a reading behind it prints a price and a
        /// row without one is visibly empty.</summary>
        private const int CostValue = 10;

        /// <summary>The stamp of the FIRST instance the stub ever built — the one a walk puts in the book
        /// before it asks for anything.</summary>
        private const string FirstInstance = "#1";

        [TestInitialize]
        public void Setup()
        {
            var localization = new Mock<ILocalizationService>();
            localization.Setup(service => service.Localize(It.IsAny<string>())).Returns<string>(key => key);
            localization
                .Setup(service => service.Render(It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<TextFormat>()))
                .Returns<string, IReadOnlyDictionary<string, object?>, TextFormat>((key, _, _) => key);
            Localization.Override(localization.Object);
        }

        [TestMethod]
        public async Task TheSheetHoldsTheAbilitiesTheCharacterOwnsAndNothingHeHasNotUnlocked()
        {
            // The screen the owner asked for shows what he HAS. An ability behind a node he never took
            // belongs to the passive wheel, which is a window of its own.
            var bench = new Bench();
            bench.Own(OwnedAbility);
            bench.Open(UnlockNode, OwnedAbility, tier: 1);

            IReadOnlyList<AbilitySocketRowView> rows = await bench.Rows();

            CollectionAssert.AreEqual(new[] { OwnedAbility }, rows.Select(row => row.AbilityId).ToArray(),
                "the sheet is not exactly the abilities the character owns");
            Assert.IsTrue(rows.Single().IsOwned);
        }

        [TestMethod]
        public async Task ASheetOfOneStanceLeavesTheOtherStancesAlone()
        {
            var bench = new Bench();
            bench.Own(OwnedAbility);
            bench.Own(StrengthAbility);

            IReadOnlyList<AbilitySocketRowView> rows = await bench.Rows(Stance.Dexterity);

            CollectionAssert.AreEqual(new[] { OwnedAbility }, rows.Select(row => row.AbilityId).ToArray());
        }

        [TestMethod]
        public async Task ARowCarriesEverySlotTheBoardOpenedForIt()
        {
            // Empty slots are the point of the screen: they are what the player drags an augment onto.
            // A row built out of what is WORN would show him a filled slot and no way to fill another.
            var bench = new Bench();
            bench.Own(OwnedAbility);
            bench.Open(UnlockNode, OwnedAbility, tier: 1);
            bench.Open(SocketNode, OwnedAbility, tier: 2);
            bench.Seat(SocketNode, OwnedAbility, tier: 2);

            AbilitySocketRowView row = (await bench.Rows()).Single();

            Assert.AreEqual(2, row.Cells.Count, "the row does not carry one cell per slot");
            Assert.AreEqual(AugmentCellKind.Empty, row.Cells[0].Kind, "the free slot is missing from the row");
            Assert.AreEqual(1, row.Cells[0].Tier, "the cells are not in the order the board gives them");
            Assert.AreEqual(AugmentCellKind.Filled, row.Cells[1].Kind);
            Assert.AreEqual(Augment, row.Cells[1].AugmentId);
            Assert.AreEqual(2, row.Cells[1].Tier, "the cell reports the tier of the augment instead of the tier of the slot");
        }

        [TestMethod]
        public async Task ACellCarriesBothTiers_TheSlotsAndTheAugmentsOwn()
        {
            // The cell shows the SLOT's tier and the card of what is in it shows the AUGMENT's, so the
            // sheet has to hand over both. Read out of the catalog the tray reads it out of: an augment's
            // tier is one number however the player happens to be looking at it.
            var bench = new Bench();
            bench.Own(OwnedAbility);
            bench.Open(UnlockNode, OwnedAbility, tier: 1);
            bench.Open(SocketNode, OwnedAbility, tier: 3);
            bench.Seat(SocketNode, OwnedAbility, tier: 3);

            AbilitySocketRowView row = (await bench.Rows()).Single();

            Assert.AreEqual(0, row.Cells[0].AugmentTier, "an empty slot named a tier of an augment it does not hold");
            Assert.AreEqual(3, row.Cells[1].Tier, "the cell lost the tier of the slot");
            Assert.AreEqual(2, row.Cells[1].AugmentTier, "the cell reports the tier of the slot instead of the tier of the augment");
        }

        [TestMethod]
        public async Task ARowKeepsItsPlaceWhileASlotStillHoldsSomethingAfterTheAbilityIsGone()
        {
            // Decision 7 of the screen, and the reason it cannot be a filter over the book: the player
            // gave the node back, the ability left the book, and his augment is still in the slot. The
            // row stays, dimmed, and every cell of it is remove-only.
            var bench = new Bench();
            bench.Open(SocketNode, OwnedAbility, tier: 2);
            bench.Seat(SocketNode, OwnedAbility, tier: 2);
            bench.Board.Sync([]);

            AbilitySocketRowView row = (await bench.Rows()).Single();

            Assert.AreEqual(OwnedAbility, row.AbilityId);
            Assert.IsFalse(row.IsOwned, "the sheet reports an ability the book does not hold as owned");
            Assert.AreEqual(AugmentCellKind.Held, row.Cells.Single().Kind, "the leftover slot is not remove-only");
            Assert.AreEqual(string.Empty, row.Cost, "an ability there is no instance of printed a live number anyway");
        }

        [TestMethod]
        public async Task TakingTheLastAugmentOutTakesTheRowWithIt()
        {
            var bench = new Bench();
            bench.Open(SocketNode, OwnedAbility, tier: 2);
            bench.Seat(SocketNode, OwnedAbility, tier: 2);
            bench.Board.Sync([]);
            Assert.AreEqual(1, (await bench.Rows()).Count, "the row was never there, so its leaving proves nothing");

            bench.Board.Extract(bench.Board.At(SocketNode));

            Assert.AreEqual(0, (await bench.Rows()).Count, "the row outlived the last augment it was holding");
        }

        [TestMethod]
        public async Task TheWheelsSheetCarriesTheAbilitiesNobodyHasUnlockedYet_EachReadOffAnInstanceOfItsOwn()
        {
            // What the passive wheel is for: the player spends it looking at nodes he has NOT bought, and
            // a node with no card is a node he cannot decide about. The reading comes from an instance
            // built and thrown away — an ability words its own description off its own numbers whether or
            // not anybody owns it.
            var bench = new Bench();
            bench.Own(OwnedAbility);

            IReadOnlyList<AbilitySocketRowView> rows = await bench.Rows(unowned: true);

            CollectionAssert.AreEqual(
                new[] { OwnedAbility, OtherOwnedAbility, NeverUnlocked, StrengthAbility }
                    .OrderBy(id => id, StringComparer.Ordinal).ToArray(),
                rows.Select(row => row.AbilityId).ToArray(),
                "the wheel was handed something other than the whole catalog");

            AbilitySocketRowView never = rows.Single(row => row.AbilityId == NeverUnlocked);
            Assert.IsFalse(never.IsOwned, "an ability nobody unlocked was reported as owned");
            Assert.AreNotEqual(string.Empty, never.Cost, "its node prints no price, so it says nothing worth reading");
            Assert.AreEqual($"{NeverUnlocked}_Description", never.Description);
            CollectionAssert.AreEqual(new[] { AbilityTags.Poison, AbilityTags.Buff }, never.Tags.ToArray(),
                "an unowned row carries no tags, so its card cannot say what the cast counts as");
        }

        [TestMethod]
        public async Task AnOwnedRowIsReadOffTheInstanceInTheBookAndNeverOffAFreshOne()
        {
            // The book's instance is the one wearing his augments. A row rebuilt from the catalog beside it
            // would print the ability he would have had if he had never seated anything — and the two only
            // differ once something IS seated, which is exactly when the difference matters most.
            var bench = new Bench();
            bench.Own(OwnedAbility);

            AbilitySocketRowView owned = (await bench.Rows(unowned: true)).Single(row => row.AbilityId == OwnedAbility);

            Assert.IsTrue(owned.IsOwned);
            Assert.AreEqual($"{OwnedAbility}{FirstInstance}", owned.DisplayName,
                "the row was built from a fresh instance instead of read off the one in the book");
        }

        [TestMethod]
        public async Task OneAbilityReadsTheSameOwnedOrNot_WhileItsSlotsAreEmpty()
        {
            // The promise of a single card: what the wheel shows him before he buys is what the socket
            // sheet shows him after, minus whatever he then seats. A second assembly for the unowned half
            // would be free to drift from the first.
            var mine = new Bench();
            mine.Own(OwnedAbility);
            var stranger = new Bench();

            AbilitySocketRowView bought = (await mine.Rows(unowned: true)).Single(row => row.AbilityId == OwnedAbility);
            AbilitySocketRowView offered = (await stranger.Rows(unowned: true)).Single(row => row.AbilityId == OwnedAbility);

            Assert.IsTrue(bought.IsOwned);
            Assert.IsFalse(offered.IsOwned);
            Assert.AreEqual(bought.Description, offered.Description, "one ability reads two ways with nothing in its slots");
            Assert.AreEqual(bought.Cost, offered.Cost);
            Assert.AreEqual(bought.Cooldown, offered.Cooldown);
            CollectionAssert.AreEqual(bought.Tags.ToArray(), offered.Tags.ToArray());
        }

        [TestMethod]
        public void ACardDoesNotPrintTheAxesEveryCastAlreadyStandsOn()
        {
            // Cost and cooldown are tags because an augment fits along them, not because they say what a
            // cast is. Printed in front of the words that do, they are what makes a reader stop reading.
            AbilityCard card = AbilityText.Card(Row("Cost: 5 Mana", "Cooldown: 3 turns",
            [
                AbilityTags.Cost, AbilityTags.Attack, AbilityTags.Cooldown, AbilityTags.Scale,
                AbilityTags.Effect, AbilityTags.Activation, AbilityTags.Poison
            ]));

            Assert.AreEqual($"{TagText.KeyOf(AbilityTags.Attack)}, {TagText.KeyOf(AbilityTags.Poison)}", card.TagsLine,
                "the card prints an axis every cast stands on, or lost a tag that names what this one is");
        }

        [TestMethod]
        public void TheCardIsTheOneReadingOfACast_Meta_Tags_AndWhatItDoes()
        {
            // Three surfaces print one ability. The card is what they all print, so what it holds and how
            // it is put together is decided here rather than three times over.
            AbilityCard card = AbilityText.Card(Row("Cost: 5 Mana", "Cooldown: 3 turns", [AbilityTags.Fire, "no_such_tag"]));

            Assert.AreEqual("Cost: 5 Mana · Cooldown: 3 turns", card.MetaLine, "price and wait are not one line");
            Assert.AreEqual($"{TagText.KeyPrefix}{AbilityTags.Fire}, {TagText.KeyPrefix}no_such_tag", card.TagsLine,
                "the tags lost their order, or a tag nobody worded went missing instead of showing its key");
            Assert.AreEqual($"{card.MetaLine}\n{card.TagsLine}\n{card.Description}", card.Body);
            Assert.AreEqual($"{card.TagsLine}\n{card.Description}", card.Details,
                "the surface that prints the meta beside the name would print it twice");
        }

        [TestMethod]
        public void ACastWithNoTagsAndNoWaitLeavesNoBlankLineBehindThem()
        {
            AbilityCard card = AbilityText.Card(Row("Cost: 5 Mana", string.Empty, []));

            Assert.AreEqual("Cost: 5 Mana", card.MetaLine, "an absent cooldown left its separator dangling");
            Assert.AreEqual(string.Empty, card.TagsLine);
            Assert.AreEqual($"Cost: 5 Mana\n{card.Description}", card.Body);
        }

        [TestMethod]
        public void TheUnlockNodePrintsTheWholeCard_AndNeverASlotAgain()
        {
            // The body of an unlock node's popup is the card's own, whole. Listing what the slots hold was
            // the popup saying everything about the sockets and nothing about the cast they sit on — the
            // ring beside the node already draws that.
            AbilityCard card = AbilityText.Card(Row("Cost: 5 Mana", "Cooldown: 3 turns", [AbilityTags.Fire]));

            string body = WheelAbilityText.AbilityBodyOf(PassiveNodeKind.AbilityUnlock, card);

            StringAssert.Contains(body, "Cost: 5 Mana", "the body lost the price of the cast");
            StringAssert.Contains(body, TagText.KeyOf(AbilityTags.Fire), "the body lost the tags");
            StringAssert.Contains(body, card.Description, "the body lost what the ability does");
            Assert.IsFalse(body.Contains("UI_PassiveTree_Slot", StringComparison.Ordinal),
                $"the body still lists slots: {body}");
            Assert.AreEqual(body, WheelAbilityText.AbilityBodyOf(PassiveNodeKind.Start, card),
                "the stance's seed hands an ability over too, and its node says nothing about it");
        }

        [TestMethod]
        public void ASocketNodeNamesWhoseSlotItIsAndReadsOutNothingElse()
        {
            // One ability is named by several nodes — the one that hands it over, and every socket node
            // that opens a slot on it. The card belongs to the first of them: pinned up under each socket
            // as well, it is the same page three times over, and the slot itself gets lost behind it.
            AbilityCard card = AbilityText.Card(Row("Cost: 5 Mana", "Cooldown: 3 turns", [AbilityTags.Fire]));

            Assert.AreEqual(string.Empty, WheelAbilityText.AbilityBodyOf(PassiveNodeKind.SocketTier2, card),
                "a socket node reads the whole ability out under a line that already names it");
            Assert.AreEqual(string.Empty, WheelAbilityText.AbilityBodyOf(PassiveNodeKind.SocketTier3, card));
            Assert.AreEqual(string.Empty, WheelAbilityText.AbilityBodyOf(PassiveNodeKind.AbilityUnlock, null),
                "a node whose card never arrived printed something anyway");
        }

        [TestMethod]
        public void TheRefusalTheWindowShowsIsTheOneTheGateGave()
        {
            // The window names a reason by asking the gate, and the gate carries the fitting rule's own
            // verdict. Nothing here measures a tier: the key is built from the answer.
            var bench = new Bench();
            bench.Open(SocketNode, OwnedAbility, tier: 2);
            IAugmentItem carried = bench.Carry(Augment);

            AugmentInstallResult refused = bench.Gate.Judge(bench.Board.At(UnlockNode), carried.InstanceId);
            Assert.AreEqual(AugmentInstallOutcome.NoSuchSocket, refused.Outcome, "a slot no node opens was judged as one that exists");
            Assert.AreEqual("UI_Augment_Refused_NoSuchSocket", AugmentRefusalText.KeyFor(refused));

            bench.Seat(SocketNode, OwnedAbility, tier: 2);
            AugmentInstallResult occupied = bench.Gate.Judge(bench.Board.At(SocketNode), bench.Carry(Augment).InstanceId);
            Assert.AreEqual(AugmentInstallOutcome.SocketOccupied, occupied.Outcome);
            Assert.AreEqual("UI_Augment_Refused_SocketOccupied", AugmentRefusalText.KeyFor(occupied));
        }

        [TestMethod]
        public void AClosedSlotIsRefusedAsGoneRatherThanAsBusy()
        {
            // The order of the gates is what the player reads. "The slot is busy" would send him
            // looking for a way to empty it; the truth is that the node behind it is gone.
            var bench = new Bench();
            bench.Open(SocketNode, OwnedAbility, tier: 2);
            string slot = bench.Board.At(SocketNode);
            bench.Seat(SocketNode, OwnedAbility, tier: 2);
            bench.Board.Sync([]);

            AugmentInstallResult refused = bench.Gate.Judge(slot, bench.Carry(Augment).InstanceId);

            Assert.AreEqual(AugmentInstallOutcome.SocketClosed, refused.Outcome);
            Assert.AreEqual("UI_Augment_Refused_SocketClosed", AugmentRefusalText.KeyFor(refused));
        }

        [TestMethod]
        public void TheGateMovesNothingWhileItIsOnlyJudging()
        {
            // The preview under the cursor runs on every mouse move. It has to be the same order of
            // checks as the install and none of its consequences.
            var bench = new Bench();
            bench.Open(SocketNode, OwnedAbility, tier: 2);
            IAugmentItem carried = bench.Carry(Augment);

            Assert.IsTrue(bench.Gate.Judge(bench.Board.At(SocketNode), carried.InstanceId).Installed,
                "the gate would not take the augment at all, so nothing below is being proved");

            Assert.IsTrue(bench.Board.Find(bench.Board.At(SocketNode))?.IsEmpty, "judging seated the augment");
            Assert.IsNotNull(bench.Bag.GetItem<IAugmentItem>(carried.InstanceId), "judging took the augment out of the bag");
        }

        /// <summary>One row of the sheet, written by hand: the card walks read the answer and never build
        /// it, so a case about the card owes nothing to the board.</summary>
        private static AbilitySocketRowView Row(string cost, string cooldown, string[] tags) =>
            new(OwnedAbility, $"{OwnedAbility}_Name", $"{OwnedAbility}_Description",
                cost, cooldown, tags, null, Stance.Dexterity, IsOwned: true, []);

        /// <summary>The records the walks name, read through the loader the game runs.</summary>
        private static string Records() =>
            $$"""
              {
                  "abilities": [ { "id": "{{OwnedAbility}}", "tags": [ "{{AbilityTags.Poison}}" ] } ],
                  "augments": [
                      { "id": "{{Augment}}", "tier": 2, "tags": [ "{{AbilityTags.Poison}}" ],
                        "upgradeProperties": { "duration": 4 } }
                  ]
              }
              """;

        /// <summary>Board, book, bag and the two things a socket screen is made of: the sheet handler
        /// and the install gate.</summary>
        private sealed class Bench
        {
            private readonly IAbilityAugmentCatalog _catalog = ShippedAbilityData.CatalogOver(Records());
            private readonly Core.Entity.Components.AbilityBookComponent _book = NewBook();
            private readonly SheetAbilityProvider _abilities = new();
            private readonly List<AbilitySocketPlacement> _open = [];
            private readonly IAugmentItemMinter _minter;

            internal Bench()
            {
                Board = new AbilitySocketBoard(_catalog);
                Bag = new SlottedBag(BagSlots);
                _minter = new AugmentItemMinter(_catalog, new AugmentMinter(
                    _catalog, new DefaultRandomNumberGenerator(Seed)));
                IPlayerAccessor players = AccessorFor(_book);
                Gate = new AugmentInstallGate(Board, new Mock<IAbilityAugmentBinder>().Object, Bag);
                Handler = new AbilitySocketRowsRequestHandler(Board, players, _abilities, _minter, _catalog, _ => null);
            }

            internal AbilitySocketBoard Board { get; }

            internal SlottedBag Bag { get; }

            internal IAugmentInstallGate Gate { get; }

            internal AbilitySocketRowsRequestHandler Handler { get; }

            /// <summary>Puts the ability in the book, the way a taken unlock node does.</summary>
            internal void Own(string abilityId) =>
                _book.Learn(_abilities.GetAbilityStance(abilityId), _abilities.CreateAbility(abilityId));

            /// <summary>Adds one slot to what the allocation opens and syncs the board to it.</summary>
            internal void Open(string socketId, string abilityId, int tier)
            {
                _open.Add(new AbilitySocketPlacement(socketId, abilityId, tier));
                Board.Sync(_open);
            }

            internal void Seat(string socketId, string abilityId, int tier) =>
                Assert.IsTrue(
                    Board.Install(new AbilitySocketPlacement(socketId, abilityId, tier).Address, Copy(Augment)),
                    $"the fixture could not fill {socketId}");

            /// <summary>A fresh copy of the record, in the bag and ready to be handed over.</summary>
            internal IAugmentItem Carry(string augmentId)
            {
                IAugmentItem? minted = _minter.Mint(augmentId);
                Assert.IsNotNull(minted, $"the records the case wrote declare no '{augmentId}'");
                Assert.IsTrue(Bag.TryAddItem(minted), "the bag would not take the augment the case is about");
                return minted;
            }

            /// <summary>What a screen asks for: the mastery window's question by default, the wheel's — the
            /// whole catalog, unlocked or not — when the case says so.</summary>
            internal Task<IReadOnlyList<AbilitySocketRowView>> Rows(Stance? stance = null, bool unowned = false) =>
                Handler.HandleRequest(new GetAbilitySocketRowsRequest(stance, IncludeUnowned: unowned));

            private const int BagSlots = 8;
            private const int Seed = 11;
            private const float Spread = 0.25f;
        }

        /// <summary>Catalog stub: four abilities, one of them of another stance, nothing hidden. Every
        /// instance it hands out is STAMPED with the order it was built in, so a walk can tell the copy in
        /// the book from one made a moment ago to read a card off.</summary>
        private sealed class SheetAbilityProvider : IAbilityProvider
        {
            private int _built;

            public IReadOnlyCollection<string> KnownAbilityIds => [OwnedAbility, OtherOwnedAbility, NeverUnlocked, StrengthAbility];

            public IAbility CreateAbility(string abilityId)
            {
                string instanceId = Guid.NewGuid().ToString();
                var ability = new Mock<IAbility>();
                ability.SetupGet(a => a.Id).Returns(abilityId);
                ability.SetupGet(a => a.InstanceId).Returns(instanceId);
                ability.SetupGet(a => a.DisplayName).Returns($"{abilityId}#{++_built}");
                ability.SetupGet(a => a.CostValue).Returns(CostValue);
                ability.SetupGet(a => a.Description).Returns($"{abilityId}_Description");
                // One tag more than the record declares: what an augment GRANTS lives on the instance.
                ability.SetupGet(a => a.Tags).Returns(new[] { AbilityTags.Poison, AbilityTags.Buff });
                ability.SetupGet(a => a.CostType).Returns(Costs.Mana);
                ability.SetupGet(a => a.InstalledUpgrades).Returns(new Dictionary<string, IAugment>());
                ability.Setup(a => a.IsSame(It.IsAny<string>())).Returns((string other) => other == instanceId);
                return ability.Object;
            }

            public IAugment? CreateUpgrade(AugmentInstance augment) => null;

            public Stance GetAbilityStance(string abilityId) =>
                abilityId == StrengthAbility ? Stance.Strength : Stance.Dexterity;

            public bool IsHidden(string abilityId) => false;
        }
    }
}

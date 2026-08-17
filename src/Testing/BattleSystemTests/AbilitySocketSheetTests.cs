namespace LastBreathTest.BattleSystemTests
{
    using Battle.Source.Abilities;
    using Battle.Source.RequestHandlers;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Data;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Inventory;
    using Core.Items;
    using Core.Localization;
    using Core.MessageBus.Requests;
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
                Handler = new AbilitySocketRowsRequestHandler(Board, players, _abilities, _minter, _ => null);
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

            internal Task<IReadOnlyList<AbilitySocketRowView>> Rows(Stance? stance = null) =>
                Handler.HandleRequest(new GetAbilitySocketRowsRequest(stance));

            private const int BagSlots = 8;
            private const int Seed = 11;
            private const float Spread = 0.25f;
        }

        /// <summary>Catalog stub: four abilities, one of them of another stance, nothing hidden.</summary>
        private sealed class SheetAbilityProvider : IAbilityProvider
        {
            public IReadOnlyCollection<string> KnownAbilityIds => [OwnedAbility, OtherOwnedAbility, NeverUnlocked, StrengthAbility];

            public IAbility CreateAbility(string abilityId)
            {
                string instanceId = Guid.NewGuid().ToString();
                var ability = new Mock<IAbility>();
                ability.SetupGet(a => a.Id).Returns(abilityId);
                ability.SetupGet(a => a.InstanceId).Returns(instanceId);
                ability.SetupGet(a => a.DisplayName).Returns(abilityId);
                ability.SetupGet(a => a.Description).Returns($"{abilityId}_Description");
                ability.SetupGet(a => a.CostType).Returns(Costs.Mana);
                ability.SetupGet(a => a.InstalledUpgrades).Returns(new Dictionary<string, IAbilityAugment>());
                ability.Setup(a => a.IsSame(It.IsAny<string>())).Returns((string other) => other == instanceId);
                return ability.Object;
            }

            public IAbilityAugment? CreateUpgrade(AugmentInstance augment) => null;

            public Stance GetAbilityStance(string abilityId) =>
                abilityId == StrengthAbility ? Stance.Strength : Stance.Dexterity;

            public bool IsHidden(string abilityId) => false;
        }
    }
}

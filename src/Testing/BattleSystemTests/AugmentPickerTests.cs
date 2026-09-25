namespace LastBreathTest.BattleSystemTests
{
    using Battle.Source.Abilities;
    using Battle.Source.RequestHandlers;
    using Core.Battle.Abilities;
    using Core.Enums;
    using Core.Items;
    using Core.Localization;
    using Core.MessageBus.Requests;
    using Core.Views;
    using Moq;

    /// <summary>
    /// What an empty slot offers when the player clicks it. The offer is the install gate's own answer
    /// asked of the whole bag — the same order of checks a drop goes through, one copy at a time — so a
    /// window can render the list without ever measuring a tier or a tag: an offer assembled beside the
    /// rule would sooner or later name an augment the gate then refuses.
    ///
    /// The order is the domain's too, and it is the order the player reads: tier down, because the tier
    /// is what a slot is spent on, then rarity down, because within one tier the rarity IS the numbers.
    /// </summary>
    [TestClass]
    public class AugmentPickerTests
    {
        private const string PoisonAbility = "Ability_Poison";

        private const string LowFit = "Augment_Poison_Tier_One";
        private const string HighFit = "Augment_Poison_Tier_Two";
        private const string AboveSlot = "Augment_Poison_Tier_Three";
        private const string WrongTag = "Augment_Cold_Tier_One";
        private const string GroupOne = "Augment_Poison_Duration";
        private const string GroupTwo = "Augment_Poison_Duration_Greater";

        private const string SlotOne = "socket_poison_one";
        private const string SlotTwo = "socket_poison_two";

        [TestInitialize]
        public void Setup()
        {
            var localization = new Mock<ILocalizationService>();
            localization.Setup(service => service.Localize(It.IsAny<string>())).Returns<string>(key => key);
            localization
                .Setup(service => service.RenderDescription(It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<TextFormat>()))
                .Returns<string, IReadOnlyDictionary<string, object?>, TextFormat>((key, _, _) => key);
            Localization.Override(localization.Object);
        }

        [TestMethod]
        public void TheSlotOffersExactlyWhatTheGateWouldTake()
        {
            // The whole point of asking the domain: the bag holds four augments and only two of them
            // belong in this slot. A window filtering on its own would have to read the tier rule and
            // the tag rule a second time to arrive here.
            var bench = new Bench();
            string slot = bench.Open(SlotOne, tier: 2);
            IAugmentItem low = bench.Carry(LowFit);
            IAugmentItem high = bench.Carry(HighFit);
            IAugmentItem tooStrong = bench.Carry(AboveSlot);
            IAugmentItem foreign = bench.Carry(WrongTag);

            IReadOnlyList<string> offered = bench.Offer(slot);

            CollectionAssert.AreEquivalent(
                new[] { high.InstanceId, low.InstanceId },
                offered.ToArray(),
                "the offer is not the copies the slot would take");
            Assert.IsTrue(offered.All(id => bench.Gate.Judge(slot, id).Installed),
                "the offer names a copy the gate would refuse");
            Assert.IsFalse(bench.Gate.Judge(slot, tooStrong.InstanceId).Installed);
            Assert.IsFalse(bench.Gate.Judge(slot, foreign.InstanceId).Installed);
        }

        [TestMethod]
        public void TheOfferIsTierDownThenRarityDown()
        {
            // Tier wins over rarity: a legendary tier-one copy sits below a common tier-two one, because
            // the slot is being spent and the tier is what it is spent on.
            var bench = new Bench();
            string slot = bench.Open(SlotOne, tier: 2);
            IAugmentItem lowCommon = bench.Carry(LowFit, Rarity.Common);
            IAugmentItem lowLegendary = bench.Carry(LowFit, Rarity.Legendary);
            IAugmentItem highCommon = bench.Carry(HighFit, Rarity.Common);
            IAugmentItem highRare = bench.Carry(HighFit, Rarity.Rare);

            IReadOnlyList<string> offered = bench.Offer(slot);

            CollectionAssert.AreEqual(
                new[] { highRare.InstanceId, highCommon.InstanceId, lowLegendary.InstanceId, lowCommon.InstanceId },
                offered.ToArray(),
                "the offer is not tier down and then rarity down");
        }

        [TestMethod]
        public void AnAugmentOfAGroupTheAbilityAlreadyWearsIsNotOffered()
        {
            // The filter is the WHOLE fitting rule and not its tier and tag halves: the exclusion group
            // is worn by the ability, so the second slot must not offer a second augment of it.
            var bench = new Bench();
            string first = bench.Open(SlotOne, tier: 2);
            string second = bench.Open(SlotTwo, tier: 2);
            IAugmentItem rival = bench.Carry(GroupTwo);
            Assert.IsTrue(bench.Offer(second).Contains(rival.InstanceId),
                "the augment was never on offer, so its leaving proves nothing");

            bench.Seat(first, GroupOne);

            CollectionAssert.DoesNotContain(bench.Offer(second).ToArray(), rival.InstanceId,
                "a slot offered an augment of a group the ability already wears");
        }

        [TestMethod]
        public void ABagWithNothingFittingOffersNothingAtAll()
        {
            // What the window says instead of opening an empty list is its own business; what the domain
            // owes it is the empty answer rather than a list it would have to check.
            var bench = new Bench();
            string slot = bench.Open(SlotOne, tier: 2);
            Assert.AreEqual(0, bench.Offer(slot).Count, "an empty bag offered something");

            bench.Carry(AboveSlot);
            bench.Carry(WrongTag);

            Assert.AreEqual(0, bench.Offer(slot).Count, "a bag holding only misfits offered one of them");
        }

        [TestMethod]
        public void ASlotThatIsNotEmptyOrNotThereOffersNothing()
        {
            // The gate's order answers this before the fitting rule is ever reached, and the offer
            // inherits it: a taken slot has nothing to be filled with, and neither has an address no
            // node opened.
            var bench = new Bench();
            string slot = bench.Open(SlotOne, tier: 2);
            bench.Carry(HighFit);
            bench.Seat(slot, LowFit);

            Assert.AreEqual(0, bench.Offer(slot).Count, "a taken slot offered a replacement");
            Assert.AreEqual(0, bench.Offer(bench.Address(SlotTwo, tier: 2)).Count, "an address nothing opened offered something");
        }

        [TestMethod]
        public void MakingTheOfferMovesNothing()
        {
            // It is a READ, run every time the player clicks an empty slot: the copies stay in the bag
            // and the slot stays empty until an install request says otherwise.
            var bench = new Bench();
            string slot = bench.Open(SlotOne, tier: 2);
            IAugmentItem carried = bench.Carry(HighFit);

            Assert.AreEqual(1, bench.Offer(slot).Count, "the offer was empty, so nothing below is being proved");

            Assert.IsNotNull(bench.Bag.GetItem<IAugmentItem>(carried.InstanceId), "the offer took the augment out of the bag");
            Assert.IsTrue(bench.Board.Find(slot)?.IsEmpty, "the offer seated the augment");
        }

        [TestMethod]
        public void WithoutACatalogTheOfferKeepsTheRestOfItsOrder()
        {
            // A composition that supplies no records seats what it is handed, so there is no tier to
            // rank by — and the offer must degrade to the rest of its order rather than to the bag's.
            // The copies are carried worst-first on purpose: an untouched bag order would pass.
            var bench = new Bench();
            string slot = bench.Open(SlotOne, tier: 2);
            IAugmentItem common = bench.Carry(HighFit, Rarity.Common);
            IAugmentItem legendary = bench.Carry(LowFit, Rarity.Legendary);

            IReadOnlyList<string> offered = [.. bench.RecordlessGate.Candidates(slot).Select(item => item.InstanceId)];

            CollectionAssert.AreEqual(
                new[] { legendary.InstanceId, common.InstanceId },
                offered.ToArray(),
                "without a catalog the offer lost its rarity order too");
        }

        [TestMethod]
        public async Task TheRequestHandsBackTheGatesOwnListInItsOwnOrder()
        {
            // The road the window actually travels. Nothing is sorted or filtered again on the way out,
            // or the picker would be showing a list of the interface's own making.
            var bench = new Bench();
            string slot = bench.Open(SlotOne, tier: 2);
            bench.Carry(LowFit, Rarity.Legendary);
            bench.Carry(HighFit, Rarity.Common);
            bench.Carry(WrongTag);

            IReadOnlyList<AugmentTrayTileView> rows = await bench.Rows(slot);

            CollectionAssert.AreEqual(
                bench.Offer(slot).ToArray(),
                rows.Select(row => row.InstanceId).ToArray(),
                "the rows are not the gate's own list in the gate's own order");
            CollectionAssert.AreEqual(new[] { 2, 1 }, rows.Select(row => row.Tier).ToArray(),
                "the rows do not carry the tier the records are written at");
        }

        /// <summary>The records the walks name, read through the loader the game runs.</summary>
        private static string Records() =>
            $$"""
              {
                  "abilities": [ { "id": "{{PoisonAbility}}", "tags": [ "{{AbilityTags.Poison}}" ] } ],
                  "augments": [
                      { "id": "{{LowFit}}", "tier": 1, "tags": [ "{{AbilityTags.Poison}}" ],
                        "upgradeProperties": { "duration": 2 } },
                      { "id": "{{HighFit}}", "tier": 2, "tags": [ "{{AbilityTags.Poison}}" ],
                        "upgradeProperties": { "duration": 4 } },
                      { "id": "{{AboveSlot}}", "tier": 3, "tags": [ "{{AbilityTags.Poison}}" ],
                        "upgradeProperties": { "duration": 6 } },
                      { "id": "{{WrongTag}}", "tier": 1, "tags": [ "{{AbilityTags.Cold}}" ],
                        "upgradeProperties": { "duration": 2 } },
                      { "id": "{{GroupOne}}", "tier": 1, "tags": [ "{{AbilityTags.Poison}}" ],
                        "exclusionGroup": "Group_Poison_Duration", "upgradeProperties": { "duration": 2 } },
                      { "id": "{{GroupTwo}}", "tier": 2, "tags": [ "{{AbilityTags.Poison}}" ],
                        "exclusionGroup": "Group_Poison_Duration", "upgradeProperties": { "duration": 3 } }
                  ]
              }
              """;

        /// <summary>Board, bag, the gate between them and the reader a window asks through.</summary>
        private sealed class Bench
        {
            private readonly IAbilityAugmentCatalog _catalog = ShippedAbilityData.CatalogOver(Records());
            private readonly List<AbilitySocketPlacement> _open = [];

            internal Bench()
            {
                Board = new AbilitySocketBoard(_catalog);
                Bag = new SlottedBag(BagSlots);
                Gate = new AugmentInstallGate(Board, new Mock<IAbilityAugmentBinder>().Object, Bag, _catalog);
                RecordlessGate = new AugmentInstallGate(Board, new Mock<IAbilityAugmentBinder>().Object, Bag);
                Handler = new CarriedAugmentsRequestHandler(_catalog, Bag, Gate, _ => null);
            }

            internal AbilitySocketBoard Board { get; }

            internal SlottedBag Bag { get; }

            internal IAugmentInstallGate Gate { get; }

            /// <summary>The same gate in a composition that supplies no records — the arrangement the
            /// socket sheet's own stand builds, and the one the tier of a copy cannot be read in.</summary>
            internal IAugmentInstallGate RecordlessGate { get; }

            private CarriedAugmentsRequestHandler Handler { get; }

            /// <summary>How the board names a slot, whether or not anything opened it.</summary>
            internal string Address(string socketId, int tier) =>
                new AbilitySocketPlacement(socketId, PoisonAbility, tier).Address;

            /// <summary>Adds one slot to what the allocation opens and syncs the board to it.</summary>
            internal string Open(string socketId, int tier)
            {
                _open.Add(new AbilitySocketPlacement(socketId, PoisonAbility, tier));
                Board.Sync(_open);
                return Address(socketId, tier);
            }

            /// <summary>A copy in the bag at a named rarity. Named rather than minted: what these walks
            /// measure is the ORDER, and a rolled rarity would put the answer in the generator.</summary>
            internal IAugmentItem Carry(string augmentId, Rarity rarity = Rarity.Common)
            {
                var item = new AugmentItem(new AugmentInstance(augmentId, new Dictionary<string, float>(), rarity));
                Assert.IsTrue(Bag.TryAddItem(item), "the bag would not take the augment the case is about");
                return item;
            }

            internal void Seat(string address, string augmentId) =>
                Assert.IsTrue(
                    Board.Install(address, new AugmentInstance(augmentId, new Dictionary<string, float>(), Rarity.Common)),
                    $"the fixture could not fill {address}");

            internal IReadOnlyList<string> Offer(string address) =>
                [.. Gate.Candidates(address).Select(item => item.InstanceId)];

            internal Task<IReadOnlyList<AugmentTrayTileView>> Rows(string address) =>
                Handler.HandleRequest(new GetAugmentCandidatesRequest(address));

            private const int BagSlots = 8;
        }
    }
}

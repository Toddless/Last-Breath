namespace LastBreathTest.Augment
{
    using Ability;
    using Battle.Source;
    using Battle.Source.Abilities;
    using Battle.Source.RequestHandlers;
    using Core.Battle.Abilities;
    using Core.Data;
    using Core.Data.GameData;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Inventory;
    using Core.Items;
    using Core.Items.Grants;
    using Core.MessageBus;
    using Core.MessageBus.Requests;
    using Core.Save;
    using Core.Save.Participants;
    using Core.Services;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using static Ability.AbilityBookStand;

    /// <summary>
    /// The road between the bag and the slot, in both directions. An augment could be found and held
    /// and a slot could be filled, but nothing joined the two: the augment a player owned had no way
    /// into a socket, which is the whole of what an augment is for.
    ///
    /// What the road costs is walked here. Seating one has to TAKE it out of the bag — a copy worn and
    /// carried at once is a copy that goes into a second slot and is sold off the build besides —
    /// while taking one out has to be the exact reverse: the same copy, with numbers nothing in the
    /// game can draw again, so a bag with no room for it refuses instead of losing it. Both roads run
    /// through one gate that answers requests, because the window that will drive them is not written
    /// yet and must not learn the rule to say why an augment stayed out.
    /// </summary>
    [TestClass]
    public class AugmentTransferTests
    {
        private const string PoisonAbility = "Ability_Test_Poison";
        private const string ColdAbility = "Ability_Test_Cold";

        private const string PoisonSlot = "socket_poison";
        private const string SecondPoisonSlot = "socket_poison_ornament";
        private const string ColdSlot = "socket_cold";

        /// <summary>Two augments of one tier and one tag, so either goes into either poison slot and
        /// what a case measures is never the fitting rule by accident.</summary>
        private const string Sharpened = "Augment_Test_Sharpened";
        private const string Ornament = "Augment_Test_Ornament";

        /// <summary>The number both records declare. Different bases, so two copies can be told apart
        /// by what they are worth as well as by the record they name.</summary>
        private const string Property = "duration";

        private const string Stackable = "Crafting_Resource_Coal";

        private const int Tier = 2;
        private const int BagSlots = 8;
        private const int Seed = 17;

        /// <summary>How many times the same augment goes in and comes back out. More than two, because
        /// a road that mints one copy per pass and a road that mints one ever read the same at two.</summary>
        private const int Passes = 5;

        /// <summary>The band the copies are drawn over. Named here rather than read off the shipped
        /// rules: a balance pass closing it must not turn these walks into claims about a file.</summary>
        private const float Spread = 0.25f;

        [TestMethod]
        public async Task AnInstalledAugmentLeavesTheBagAndSitsInTheSlot()
        {
            // The whole point of the gate, and the half that cannot be skipped: the item goes out of
            // the bag. Left in it, one draw would be worn and carried at once — seated a second time
            // in the next slot, and sold while the character is still wearing it.
            var bench = new Bench();
            IAugmentItem held = bench.Held(Sharpened);

            AugmentInstallResult result = await bench.Install(PoisonSlot, held.InstanceId);

            Assert.IsTrue(result.Installed, $"the augment stayed out of the slot: {result.Outcome} / {result.Fit}");
            Assert.AreSame(held.Augment, bench.Board.Find(bench.Board.At(PoisonSlot))?.Augment,
                "the slot holds something other than the copy that was handed over");
            Assert.IsNull(bench.Bag.GetItem<IAugmentItem>(held.InstanceId), "the seated augment is lying in the bag as well");
            Assert.AreEqual(0, bench.Bag.GetContents().Count, "the bag still carries something after giving up its only augment");
        }

        [TestMethod]
        public async Task AnAugmentAlreadySeatedCannotBeSeatedASecondTime()
        {
            // The other side of the same rule, on the road it would actually be broken on: the window
            // offers what the bag holds, and a copy that never left it would be offered to every
            // remaining slot in turn.
            var bench = new Bench();
            IAugmentItem held = bench.Held(Sharpened);
            Assert.IsTrue((await bench.Install(PoisonSlot, held.InstanceId)).Installed, "the first seating never happened");

            AugmentInstallResult again = await bench.Install(SecondPoisonSlot, held.InstanceId);

            Assert.AreEqual(AugmentInstallOutcome.AugmentNotHeld, again.Outcome);
            Assert.IsTrue(bench.Board.Find(bench.Board.At(SecondPoisonSlot))?.IsEmpty, "one copy of the augment ended up in two slots at once");
        }

        [TestMethod]
        public async Task TwoCopiesOfARecordFillTwoSlotsWhereOneCopyOnlyEverFillsOne()
        {
            // The pair the rule has to tell apart, walked in one go so neither half can be read as the
            // other's accident: what a slot takes is a COPY, and the player owning two of a record is
            // entitled to wear both. Only the copy already seated is refused — refusing the record
            // would charge him for owning two, and letting the copy through twice is the duplication.
            var bench = new Bench();
            IAugmentItem first = bench.Held(Sharpened);
            IAugmentItem second = bench.Held(Sharpened);
            Assert.AreNotEqual(first.InstanceId, second.InstanceId, "the bag handed out one copy twice, so nothing below is about two");

            Assert.IsTrue((await bench.Install(PoisonSlot, first.InstanceId)).Installed, "the first copy never reached its slot");
            AugmentInstallResult sameAgain = await bench.Install(SecondPoisonSlot, first.InstanceId);
            Assert.IsTrue((await bench.Install(SecondPoisonSlot, second.InstanceId)).Installed, "the second copy was refused the free slot");

            Assert.AreEqual(AugmentInstallOutcome.AugmentNotHeld, sameAgain.Outcome, "the seated copy was offered a second slot and taken");
            Assert.AreSame(first.Augment, bench.Board.Find(bench.Board.At(PoisonSlot))?.Augment, "the first slot holds something other than the first copy");
            Assert.AreSame(second.Augment, bench.Board.Find(bench.Board.At(SecondPoisonSlot))?.Augment, "the second slot holds something other than the second copy");
            Assert.AreEqual(0, bench.Bag.GetContents().Count, "a copy is worn and carried at once");
        }

        [TestMethod]
        public async Task SeatingAndTakingBackTheSameAugmentOverAndOverLeavesThePlayerOwningOneOfIt()
        {
            // The property the two roads are worth nothing without, and the one a window driving them
            // breaks first: an augment is a THING, so the number of them the player owns is the same
            // after a hundred passes as before the first. A seating that leaves the copy in the bag,
            // or an extraction that mints beside what is already carried, shows up here as a count and
            // not as a wrong number — which is what makes it visible at all.
            var bench = new Bench();
            IAugmentItem held = bench.Held(Sharpened);
            float rolled = held.Augment.Values[Property];

            for (int pass = 0; pass < Passes; pass++)
            {
                // Read afresh every pass: the copy comes home in a new wrapper, and a walk holding on
                // to the first id would be asking about a passage that ended rather than about a thing.
                string carried = TheOnlyAugmentIn(bench.Bag).InstanceId;

                Assert.IsTrue((await bench.Install(PoisonSlot, carried)).Installed, $"pass {pass}: the augment never reached the slot");
                Assert.AreEqual(0, bench.Bag.GetContents().Count, $"pass {pass}: the seated copy is lying in the bag as well");
                // Asked of the bag the way the gate asks it — by the copy's own key. A bag answering
                // here for something no slot of it holds is a bag that will hand the copy to the next
                // socket too, which is the whole of the duplication.
                Assert.IsNull(bench.Bag.GetItem<IAugmentItem>(carried), $"pass {pass}: the bag still answers for the copy it gave up");
                Assert.AreEqual(AugmentExtractResult.Extracted, await bench.Extract(PoisonSlot), $"pass {pass}: the augment never left the slot");
            }

            IAugmentItem ended = TheOnlyAugmentIn(bench.Bag);
            Assert.AreEqual(Sharpened, ended.Id, "what the player is left holding is another augment");
            Assert.AreEqual(rolled, ended.Augment.Values[Property], "the copy came back at another number than it was drawn at");
            Assert.IsTrue(bench.Board.Sockets.All(socket => socket.IsEmpty), "a slot kept an augment the player took back");
        }

        [TestMethod]
        public async Task AnExtractedAugmentComesBackToTheBagAsTheVerySameCopy()
        {
            // An augment IS its numbers: they were drawn once, and a return that rebuilt it from the
            // record would hand the player a different augment wearing the same name.
            var bench = new Bench();
            IAugmentItem held = bench.Held(Sharpened);
            float rolled = held.Augment.Values[Property];
            await bench.Install(PoisonSlot, held.InstanceId);

            AugmentExtractResult result = await bench.Extract(PoisonSlot);

            Assert.AreEqual(AugmentExtractResult.Extracted, result);
            Assert.IsTrue(bench.Board.Find(bench.Board.At(PoisonSlot))?.IsEmpty, "the augment is in the bag and in the slot at once");
            IAugmentItem returned = TheOnlyAugmentIn(bench.Bag);
            Assert.AreSame(held.Augment, returned.Augment, "what came back is another copy of the record, not the one that went in");
            Assert.AreEqual(rolled, returned.Augment.Values[Property], "the augment came back at another number");
        }

        [TestMethod]
        public async Task AFullBagLeavesTheAugmentInTheSlotInsteadOfLosingItOnTheWayHome()
        {
            // The one refusal that has to be safe rather than merely correct: there is nowhere to put
            // the augment down, and the numbers cannot be drawn again, so the extraction does not
            // happen at all. The augment stays worn, which is a state the player can act on.
            var bench = new Bench(slots: 1);
            IAugmentItem held = bench.Held(Sharpened);
            float rolled = held.Augment.Values[Property];
            await bench.Install(PoisonSlot, held.InstanceId);
            Assert.IsTrue(bench.Bag.TryAddItem(Resource()), "the bag under test has room to spare, so nothing below is being refused");

            AugmentExtractResult result = await bench.Extract(PoisonSlot);

            Assert.AreEqual(AugmentExtractResult.NoBagRoom, result);
            Assert.AreEqual(Sharpened, bench.Board.Find(bench.Board.At(PoisonSlot))?.Augment?.AugmentId,
                "the augment left the slot on its way to a bag that could not take it");
            Assert.AreEqual(rolled, bench.Board.Find(bench.Board.At(PoisonSlot))?.Augment?.Values[Property], "the augment left its numbers behind");
            Assert.AreEqual(1, bench.Bag.GetContents().Count, "the bag took the augment after saying it could not");
        }

        [TestMethod]
        public async Task AnAugmentThatDoesNotBelongInTheSlotStaysInTheBagAndTheRuleSaysWhy()
        {
            // The refusal is named in the rule's own words. A window told only "no" would have to
            // measure tiers and tags itself to explain it, and two readings of one rule are two
            // answers — one offering what the other refuses.
            var bench = new Bench();
            IAugmentItem held = bench.Held(Sharpened);

            AugmentInstallResult refused = await bench.Install(ColdSlot, held.InstanceId);

            Assert.AreEqual(AugmentInstallOutcome.DoesNotFit, refused.Outcome);
            Assert.AreEqual(AugmentFitResult.NoSharedTag, refused.Fit, "the caller is told that it did not fit and never why");
            Assert.IsTrue(bench.Board.Find(bench.Board.At(ColdSlot))?.IsEmpty, "the refused augment went in anyway");
            Assert.IsNotNull(bench.Bag.GetItem<IAugmentItem>(held.InstanceId), "the refused augment left the bag all the same");
            Assert.IsTrue((await bench.Install(PoisonSlot, held.InstanceId)).Installed,
                "the augment goes into no slot at all, so the refusal above says nothing about the rule");
        }

        [TestMethod]
        public async Task AnOccupiedSlotIsAnAnswerOfItsOwnAndKeepsWhatIsInIt()
        {
            // A full slot is not a misfit, and the difference is what the player is told: one asks him
            // to take something out first, the other tells him the augment does not belong there.
            var bench = new Bench();
            IAugmentItem seated = bench.Held(Sharpened);
            IAugmentItem offered = bench.Held(Ornament);
            await bench.Install(PoisonSlot, seated.InstanceId);

            AugmentInstallResult refused = await bench.Install(PoisonSlot, offered.InstanceId);

            Assert.AreEqual(AugmentInstallOutcome.SocketOccupied, refused.Outcome);
            Assert.IsNull(refused.Fit, "a slot that was never asked about fitting came back with a verdict");
            Assert.AreSame(seated.Augment, bench.Board.Find(bench.Board.At(PoisonSlot))?.Augment, "the occupant was pushed out of its slot");
            Assert.IsNotNull(bench.Bag.GetItem<IAugmentItem>(offered.InstanceId), "the refused augment left the bag");
        }

        [TestMethod]
        public async Task ASlotNoNodeOpensIsToldApartFromAnAugmentThatDoesNotBelong()
        {
            // Both are refusals of the same request, and neither is the other's business: a socket id
            // nothing opens is a window looking at a build the character no longer owns.
            var bench = new Bench();
            IAugmentItem held = bench.Held(Sharpened);

            AugmentInstallResult refused = await bench.Install("socket_no_node_opens", held.InstanceId);

            Assert.AreEqual(AugmentInstallOutcome.NoSuchSocket, refused.Outcome);
            Assert.IsNotNull(bench.Bag.GetItem<IAugmentItem>(held.InstanceId), "the augment left the bag for a slot that does not exist");
        }

        [TestMethod]
        public async Task ACopyIsNeitherDoubledNorLostAcrossASaveAndLoadInEitherState()
        {
            // The two states are written by two sections, and the file is the one place where a copy
            // could quietly become two: seated and carried at once, or written by neither and gone.
            var bench = new Bench();
            IAugmentItem carried = bench.Held(Sharpened);
            IAugmentItem seated = bench.Held(Ornament);
            Assert.IsTrue((await bench.Install(PoisonSlot, seated.InstanceId)).Installed, "nothing is in a slot, so only one state is being saved");
            float carriedRoll = carried.Augment.Values[Property];
            float seatedRoll = seated.Augment.Values[Property];

            JToken bagFile = Written(bench.BagSection().Capture()); // through the file, not around it
            JToken bookFile = Written(bench.BookSection().Capture());

            var target = new Bench();
            target.BagSection().Restore(bagFile, target.BagSection().Version);
            target.BookSection().Restore(bookFile, target.BookSection().Version);

            AugmentInstance? restoredSeat = target.Board.Find(target.Board.At(PoisonSlot))?.Augment;
            Assert.AreEqual(Ornament, restoredSeat?.AugmentId, "the slot came back holding another augment, or none");
            Assert.AreEqual(seatedRoll, restoredSeat?.Values[Property], "the seated copy came back at another number");
            IAugmentItem restoredBag = TheOnlyAugmentIn(target.Bag);
            Assert.AreEqual(Sharpened, restoredBag.Id, "the augment that was in the slot is lying in the bag as well");
            Assert.AreEqual(carriedRoll, restoredBag.Augment.Values[Property], "the carried copy came back at another number");
        }

        [TestMethod]
        public void BothRoadsAreGatesTheCompositionRegisters()
        {
            // The window of the next step must not reach for the board itself: all it can do is send a
            // request, and the composition is what answers one. Nothing else in the game builds these
            // handlers, so a gate left unregistered is a road with no way onto it.
            IServiceCollection services = new ServiceCollection()
                .AddSharedGameDataParticipants()
                .AddBattleSystemModuleDependencies();
            services.AddSingleton<IInventory>(new SlottedBag(BagSlots));
            services.AddSingleton<IRandomNumberGenerator>(new DefaultRandomNumberGenerator(Seed));
            // Whose abilities the seated augment reaches: the accessor is a shared service of every
            // project, and the module composed without one would build no binder for the gates.
            services.AddSingleton<IPlayerAccessor>(new PlayerAccessor());
            using ServiceProvider container = services.BuildServiceProvider();

            Assert.IsNotNull(container.GetService<IRequestHandler<InstallAugmentRequest, AugmentInstallResult>>(),
                "an install can be asked for and nothing answers");
            Assert.IsNotNull(container.GetService<IRequestHandler<ExtractAugmentRequest, AugmentExtractResult>>(),
                "an extraction can be asked for and nothing answers");
        }

        /// <summary>The only augment the bag is carrying — asserted to be the only thing in it, because
        /// a copy that was doubled shows up as a second entry and not as a wrong one.</summary>
        private static IAugmentItem TheOnlyAugmentIn(IInventory bag)
        {
            IReadOnlyList<(IItem Item, int Amount)> contents = bag.GetContents();
            Assert.AreEqual(1, contents.Count, "the bag holds something besides the one augment the case is about");
            var augment = contents[0].Item as IAugmentItem;
            Assert.IsNotNull(augment, "what the bag holds is not an augment");
            return augment;
        }

        /// <summary>A section as it lands on disk, read back the way a load reads it.</summary>
        private static JToken Written(JToken captured) => JToken.Parse(captured.ToString(Formatting.None));

        /// <summary>A plain template of the kind the bag piles up, for filling it.</summary>
        private static CraftingResource Resource() => new(Stackable, 20, [], null!, Rarity.Common);

        /// <summary>Two abilities sharing nothing, and two augments of the poison one. Written by the
        /// test and read through the game's own loader: the fitting rule reads fields a hand-built
        /// record cannot prove are ever parsed, and a pair of its own keeps these walks about the road
        /// rather than about whatever the shipped markup is repointed at.</summary>
        private static string Records() =>
            $$"""
              {
                  "abilities": [
                      { "id": "{{PoisonAbility}}", "tags": [ "{{AbilityTags.Poison}}" ] },
                      { "id": "{{ColdAbility}}", "tags": [ "{{AbilityTags.Cold}}" ] }
                  ],
                  "augments": [
                      { "id": "{{Sharpened}}", "tier": {{Tier}}, "tags": [ "{{AbilityTags.Poison}}" ],
                        "upgradeProperties": { "{{Property}}": 4 } },
                      { "id": "{{Ornament}}", "tier": {{Tier}}, "tags": [ "{{AbilityTags.Poison}}" ],
                        "upgradeProperties": { "{{Property}}": 9 } }
                  ]
              }
              """;

        /// <summary>The whole road standing up: the records, a bag, the slots an allocation opened, the
        /// door augments are minted through and the two gates between bag and board.</summary>
        private sealed class Bench
        {
            private readonly IAbilityAugmentCatalog _catalog = ShippedAbilityData.CatalogOver(Records());

            internal Bench(int slots = BagSlots)
            {
                Bag = new SlottedBag(slots);
                Board = new AbilitySocketBoard(_catalog);
                Board.Sync(
                [
                    new AbilitySocketPlacement(PoisonSlot, PoisonAbility, Tier),
                    new AbilitySocketPlacement(SecondPoisonSlot, PoisonAbility, Tier),
                    new AbilitySocketPlacement(ColdSlot, ColdAbility, Tier),
                ]);
                Augments = new AugmentItemMinter(_catalog, new AugmentMinter(_catalog,
                    new DefaultRandomNumberGenerator(Seed)));
                Binder = new AbilityAugmentBinder(new PlayerAccessor(), Board, new Mock<IAbilityProvider>().Object);
            }

            internal SlottedBag Bag { get; }

            internal AbilitySocketBoard Board { get; }

            internal AugmentItemMinter Augments { get; }

            /// <summary>What the gates hand the board over to. This file is about the road between bag
            /// and slot, so the binder stands over a character with no abilities at all — what a seated
            /// augment DOES is walked where the abilities are (<see cref="AugmentEffectTests"/>).</summary>
            internal IAbilityAugmentBinder Binder { get; }

            /// <summary>A fresh copy of the record, in the bag and ready to be handed over.</summary>
            internal IAugmentItem Held(string augmentId)
            {
                IAugmentItem? minted = Augments.Mint(augmentId);
                Assert.IsNotNull(minted, $"the records the case wrote declare no '{augmentId}'");
                Assert.IsTrue(Bag.TryAddItem(minted), "the bag would not take the augment the case is about");
                return minted;
            }

            internal Task<AugmentInstallResult> Install(string socketId, string itemInstanceId) =>
                new InstallAugmentRequestHandler(new AugmentInstallGate(Board, Binder, Bag))
                    .HandleRequest(new InstallAugmentRequest(Board.At(socketId), itemInstanceId));

            internal Task<AugmentExtractResult> Extract(string socketId) =>
                new ExtractAugmentRequestHandler(Board, Augments, Bag, Binder)
                    .HandleRequest(new ExtractAugmentRequest(Board.At(socketId)));

            /// <summary>The bag section as the game registers it, minus the item data: an augment must
            /// travel as an augment, and a strict provider fails the walk the moment one is written
            /// down or read back as a resource id.</summary>
            internal InventorySaveParticipant BagSection() =>
                new(Bag, new Mock<IItemDataProvider>(MockBehavior.Strict).Object,
                    new EquipItemSaveConverter(new GrantFactory(() => null, () => null, () => null)), Augments);

            /// <summary>The section that carries the slots. It needs a player to write anything at
            /// all — the book hangs off one — so one is stood up beside the board.</summary>
            internal AbilityBookSaveParticipant BookSection() => new(AccessorFor(NewBook()), Board);
        }
    }
}

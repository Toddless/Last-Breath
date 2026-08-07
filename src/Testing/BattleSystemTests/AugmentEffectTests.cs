namespace LastBreathTest.BattleSystemTests
{
    using Battle.Source;
    using Battle.Source.Abilities;
    using Battle.Source.RequestHandlers;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Data.AbilityData;
    using Core.Data.GameData;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Events;
    using Core.Inventory;
    using Core.Items;
    using Core.MessageBus.Requests;
    using Core.PassiveTree;
    using Core.PassiveTree.Allocation;
    using Core.Save;
    using Core.Save.Participants;
    using Core.Services;
    using Core.Session;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using static AbilityBookStand;
    using static AugmentCopies;

    /// <summary>
    /// An augment in a slot has to DO something. Everything before this step could be found, carried,
    /// judged, seated and saved, and the ability it was seated on went on casting exactly as it had
    /// before — the arrangement was a picture of a build rather than the build itself.
    ///
    /// What is walked here is that the sockets are now the whole of it, down every road the board is
    /// changed by. The player's own hands are only one of them: an allocation that takes a node back
    /// drops the slot and the augment in it, a load lays a saved arrangement out, a new playthrough
    /// empties the board. Each of those has to reach the ability, because an upgrade left standing
    /// after its slot is gone is a build the player did not pay for and cannot see.
    ///
    /// And it has to be the COPY that works, not the record: the numbers are drawn once, per copy, and
    /// an ability wearing the record's averages would be an ability doing something other than what its
    /// owner's tooltip says.
    /// </summary>
    [TestClass]
    public class AugmentEffectTests
    {
        /// <summary>Head Butt: it costs mana and it carries the tag the augment below is written for.</summary>
        private const string AbilityId = "Ability_Head_Butt";

        /// <summary>A shipped augment that moves a number every ability publishes — what the cast
        /// charges — so what the seating does is readable through the ability's own contract instead of
        /// through a member of one class. A longer stun bought with more mana, tier 2.</summary>
        private const string Augment = "Augment_Extend_Stun_Add_Cost";

        /// <summary>The property of that record which lands on the price of the cast.</summary>
        private const string CostProperty = "additionalCost";

        private const string Seed = "start";
        private const string UnlockNode = "abilityunlock_head_butt";
        private const string SocketNode = "sockettier2_head_butt";

        private const int BagSlots = 4;

        [TestMethod]
        public void SeatingAnAugmentChangesWhatTheAbilityDoes()
        {
            // The whole point of the step: before it, this walk passed with the cast unchanged.
            var bench = new Bench();
            int bare = bench.Ability.CostValue;
            float rolled = bench.Declared * 2f;

            Assert.IsTrue(bench.Seat(Copy(Augment, (CostProperty, rolled))), "the augment never reached the slot");

            Assert.AreEqual(bare + (int)rolled, bench.Ability.CostValue, "the augment is in the slot and the cast costs what it always did");
            Assert.AreEqual(1, bench.Ability.InstalledUpgrades.Count, "the ability does not know it is wearing anything");
            Assert.AreEqual(Augment, bench.Ability.InstalledUpgrades[SocketNode].Id, "the slot is worn by something else");
        }

        [TestMethod]
        public void TakingTheAugmentOutGivesTheAbilityBackWhatItWas()
        {
            // The mirror, and the one that catches an upgrade applied twice or never taken off: the
            // ability has to land exactly where it started rather than merely lower than it was.
            var bench = new Bench();
            int bare = bench.Ability.CostValue;
            Assert.IsTrue(bench.Seat(Copy(Augment, (CostProperty, bench.Declared * 2f))), "the augment never reached the slot");

            Assert.AreEqual(AugmentExtractResult.Extracted, bench.Unseat(), "the augment never left the slot");

            Assert.AreEqual(bare, bench.Ability.CostValue, "the cast kept paying for an augment that is back in the bag");
            Assert.AreEqual(0, bench.Ability.InstalledUpgrades.Count, "the ability still counts the augment it gave up");
        }

        [TestMethod]
        public void TheAbilityWorksAtTheNumbersTheSeatedCopyRolledAndNotTheRecordsAverage()
        {
            // Two copies of one record, worth different amounts, on two characters holding the same
            // ability. Built from the record instead, both would charge the same — and the tooltip of
            // the lucky copy would be advertising a number nothing in the game ever applies.
            var lucky = new Bench();
            var poor = new Bench();
            int bare = lucky.Ability.CostValue;

            Assert.IsTrue(lucky.Seat(Copy(Augment, (CostProperty, lucky.Declared * 2f))), "the lucky copy never reached the slot");
            Assert.IsTrue(poor.Seat(Copy(Augment, (CostProperty, poor.Declared))), "the average copy never reached the slot");

            Assert.AreEqual(bare + (int)(lucky.Declared * 2f), lucky.Ability.CostValue, "the lucky copy works at the record's number");
            Assert.AreEqual(bare + (int)poor.Declared, poor.Ability.CostValue, "the average copy works at something other than what it rolled");
            Assert.AreNotEqual(lucky.Ability.CostValue, poor.Ability.CostValue, "two copies worth different amounts do the same thing");
        }

        [TestMethod]
        public void GivingTheNodeBackTakesTheAugmentOffTheAbility()
        {
            // The road nobody drives on purpose. The slot is the node's, and refunding it drops the
            // slot together with what stood in it — so the ability has to stop wearing the augment in
            // the same pass, without any hand having touched the socket.
            var bench = new Bench();
            int bare = bench.Ability.CostValue;
            Assert.IsTrue(bench.Seat(Copy(Augment, (CostProperty, bench.Declared * 2f))), "the augment never reached the slot");
            Assert.AreNotEqual(bare, bench.Ability.CostValue, "the seating did nothing, so the refund below proves nothing");

            Assert.AreEqual(AllocationResult.Success, bench.Tree.Refund(SocketNode));

            Assert.IsNull(bench.Board.Find(SocketNode), "the slot outlived the point that paid for it");
            Assert.AreEqual(bare, bench.Ability.CostValue, "the node came back and the ability kept the augment it opened");
            Assert.AreEqual(0, bench.Ability.InstalledUpgrades.Count);
        }

        [TestMethod]
        public void ALoadPutsBackExactlyWhatWasSeatedWithTheNumbersThatCopyRolled()
        {
            // A load has to end with the augment WORKING, not merely remembered. It travels the same
            // binder a player's own seating does, so what comes back is the copy's own numbers rather
            // than the record's — the file is the only place those numbers survive at all.
            var source = new Bench();
            float rolled = source.Declared * 2f;
            Assert.IsTrue(source.Seat(Copy(Augment, (CostProperty, rolled))), "the augment never reached the slot");
            int worn = source.Ability.CostValue;

            var target = new Bench(taken: false);
            target.Save().Restore(source.Save().Capture(new SaveMetadata()));

            Assert.AreEqual(Augment, target.Board.Find(SocketNode)?.Augment?.AugmentId, "the slot came back empty");
            Assert.AreEqual(worn, target.Ability.CostValue, "the load brought the augment back and the ability does not wear it");
            Assert.AreEqual(rolled, target.Board.Find(SocketNode)?.Augment?.Values[CostProperty], "the copy came back at another number");
        }

        [TestMethod]
        public void ANewPlaythroughLeavesNoAugmentOnTheAbility()
        {
            // The board is a singleton and outlives the scene. Emptying it is not enough on its own:
            // the ability of the playthrough being left behind would go on wearing what the board no
            // longer holds, so the abilities are undressed in the same reset — and after the board,
            // which is what the registration order is for.
            var bench = new Bench();
            int bare = bench.Ability.CostValue;
            Assert.IsTrue(bench.Seat(Copy(Augment, (CostProperty, bench.Declared * 2f))), "the augment never reached the slot");
            Assert.AreNotEqual(bare, bench.Ability.CostValue, "the seating did nothing, so the reset below proves nothing");

            bench.NewSession().ResetSession();

            Assert.AreEqual(bare, bench.Ability.CostValue, "the new playthrough starts with the previous one's augment still working");
            Assert.AreEqual(0, bench.Ability.InstalledUpgrades.Count);
        }

        [TestMethod]
        public void TheOnlyRoadOntoAnAbilityIsTheOneTheCompositionRegisters()
        {
            // Nothing else in the game may decide what an ability wears, so the binder has to be a
            // service of the module rather than something a handler builds for itself — a second
            // instance would be a second opinion about the same board.
            IServiceCollection services = new ServiceCollection()
                .AddSharedGameDataParticipants()
                .AddBattleSystemModuleDependencies();
            services.AddSingleton<IPlayerAccessor>(new PlayerAccessor());
            using ServiceProvider container = services.BuildServiceProvider();

            var binder = container.GetService<IAbilityAugmentBinder>();

            Assert.IsNotNull(binder, "the board has no road onto the abilities at all");
            Assert.AreSame(binder, container.GetService<IAbilityAugmentBinder>(), "every caller gets a binder of its own");
            Assert.IsInstanceOfType<ISessionResettable>(binder, "a new playthrough cannot undress the abilities of the old one");
        }

        /// <summary>
        /// One character: the shipped abilities and augment records, a tree granting Head Butt and a
        /// tier-2 slot on it, a bag, and the gates an augment travels between the two. Everything is
        /// the real thing — the walks above are about behaviour reaching an ability, and a stand-in
        /// ability would be a walk about the stand-in.
        /// </summary>
        private sealed class Bench
        {
            private readonly AbilityProvider _abilities;
            private readonly AbilityAugmentCatalog _catalog;
            private readonly IPlayerAccessor _players;
            private readonly SlottedBag _bag;
            private readonly IAugmentItemMinter _minter;
            private readonly IAbilityAugmentBinder _binder;

            /// <param name="taken">Whether the character has spent his points. False stands for the
            /// launch a load lands in: the same build, nothing taken yet, the tree section about to
            /// hand the allocation over.</param>
            internal Bench(bool taken = true)
            {
                (_abilities, _catalog) = ShippedAbilityData.Load();
                _players = AccessorFor(BookOverAFighter());
                _bag = new SlottedBag(BagSlots);
                _minter = new AugmentItemMinter(_catalog, new AugmentMinter(
                    _catalog, new StubCombatRules(AugmentValueRules.Fixed), new DefaultRandomNumberGenerator(seed: 1)));

                Board = new AbilitySocketBoard(_catalog);
                Tree = NewTree();
                _binder = new AbilityAugmentBinder(_players, Board, _abilities);
                Unlock = new AbilityUnlockService(_players, _abilities, Tree, Board, _binder);

                if (!taken) return;

                Tree.Take(UnlockNode);
                Tree.Take(SocketNode);
            }

            internal PassiveTreeService Tree { get; }

            internal AbilitySocketBoard Board { get; }

            internal AbilityUnlockService Unlock { get; }

            /// <summary>The number the record itself declares — read off the shipped catalog, so a
            /// balance pass moves the walks with it instead of breaking them.</summary>
            internal float Declared => Record().UpgradeProperties[CostProperty];

            /// <summary>The learned ability under test. Read fresh every time: the book hands out the
            /// instance it holds, and a load builds the character's abilities anew.</summary>
            internal IAbility Ability
            {
                get
                {
                    IAbility? learned = _players.Player?.AbilityBook.AllAbilities.FirstOrDefault(ability => ability.Id == AbilityId);
                    Assert.IsNotNull(learned, $"the character never learned '{AbilityId}'");
                    return learned;
                }
            }

            /// <summary>Puts a copy into the tier-2 slot the way the player does: into the bag, then
            /// through the gate that answers an install request.</summary>
            internal bool Seat(AugmentInstance copy)
            {
                IAugmentItem? item = _minter.Restore(copy);
                Assert.IsNotNull(item, $"the shipped catalog declares no '{copy.AugmentId}'");
                Assert.IsTrue(_bag.TryAddItem(item), "the bag would not take the augment the case is about");

                return new InstallAugmentRequestHandler(Board, _bag, _binder)
                    .HandleRequest(new InstallAugmentRequest(SocketNode, item.InstanceId))
                    .Result.Installed;
            }

            /// <summary>And back out, through the mirror gate.</summary>
            internal AugmentExtractResult Unseat() =>
                new ExtractAugmentRequestHandler(Board, _minter, _bag, _binder)
                    .HandleRequest(new ExtractAugmentRequest(SocketNode))
                    .Result;

            /// <summary>The two sections that carry a character's build, in the order the manager
            /// restores them: the allocation grants the abilities and the slots, the book fills them.</summary>
            internal ISaveManager Save()
            {
                var manager = new SaveManager(new LoadScope());
                manager.Register(new PassiveTreeSaveParticipant(Tree));
                manager.Register(new AbilityBookSaveParticipant(_players, Board, _binder));
                return manager;
            }

            /// <summary>The reset stack the game builds, over the board and what carries it onto the
            /// abilities. Registered through the real composition, so the order between the two is the
            /// game's own and not the case's.</summary>
            internal ISessionResetService NewSession()
            {
                var services = new ServiceCollection();
                services.AddSingleton<LoadScope>();
                services.AddSingleton<IAbilitySocketBoard>(Board);
                services.AddSingleton(_binder);
                services.AddSessionReset();
                return services.BuildServiceProvider().GetRequiredService<ISessionResetService>();
            }

            /// <summary>A book on a fighter real enough to be given an ability: learning one hands it
            /// its owner, and the ability subscribes to the owner's combat bus on the spot.</summary>
            private static AbilityBookComponent BookOverAFighter()
            {
                var owner = new Mock<IFightable>();
                owner.SetupGet(fighter => fighter.CombatEvents).Returns(new CombatEventBus());
                return new AbilityBookComponent(owner.Object);
            }

            private AbilityUpgradeData Record()
            {
                AbilityUpgradeData? record = _catalog.Find(Augment);
                Assert.IsNotNull(record, $"the shipped data declares no '{Augment}'");
                return record;
            }

            /// <summary>An unlock node for the ability and a tier-2 socket node on it, both hanging off
            /// the seed. The unlock node brings the tier-1 slot with it, which the tier-2 augment does
            /// not fit — so the slot under test is unmistakably the one the socket node opened.</summary>
            private PassiveTreeService NewTree()
            {
                var document = new PassiveTreeDocument();
                document.AddNode(new PassiveNode { Id = Seed, Kind = PassiveNodeKind.Start, Stance = Core.Enums.Stance.Strength });
                Add(UnlockNode, PassiveNodeKind.AbilityUnlock);
                Add(SocketNode, PassiveNodeKind.SocketTier2);

                var service = new PassiveTreeService(new AbilityTreeStub(document), ConditionCatalogs.Empty());
                service.SetTotalPoints(document.Nodes.Count);
                return service;

                void Add(string id, PassiveNodeKind kind)
                {
                    document.AddNode(new PassiveNode { Id = id, Kind = kind, AbilityId = AbilityId });
                    document.Link(Seed, id);
                }
            }
        }

        /// <summary>Stands in for the data pipeline: the document is already parsed.</summary>
        private sealed class AbilityTreeStub(PassiveTreeDocument tree) : IPassiveTreeProvider
        {
            public PassiveTreeDocument Tree { get; } = tree;

            public IReadOnlyList<string> Issues => [];
        }
    }
}

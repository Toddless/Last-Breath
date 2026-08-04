namespace LastBreathTest.BattleSystemTests
{
    using Battle.Source;
    using Core.Battle.Abilities;
    using Core.Data.SaveData;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.PassiveTree;
    using Core.PassiveTree.Allocation;
    using Core.Save;
    using Core.Save.Participants;
    using Core.Services;
    using Core.Session;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Newtonsoft.Json.Linq;
    using static AugmentCopies;

    /// <summary>
    /// An augment slot exists exactly while the node that opened it is taken, and what sits in it is
    /// the only part of the arrangement the save file remembers. The slot is addressed by its own id
    /// rather than by its tier, so an ability can carry two slots of the same tier â€” and each saved
    /// augment carries the slot it was chosen for, so an id the next build points elsewhere is not
    /// mistaken for the slot it came out of.
    /// </summary>
    [TestClass]
    public class AbilitySocketTests
    {
        private const string Seed = "start_1";
        private const string DexNode = "abilityunlock_dex";
        private const string SocketTwo = "sockettier2_dex";
        private const string SocketTwoOrnament = "sockettier2_dex_ornament";
        private const string SocketThree = "sockettier3_dex";
        private const string GhostSocket = "sockettier2_ghost";

        private const string DexAbility = "Ability_Dex";
        private const string DexAbilityTwo = "Ability_Dex_Two";
        private const string GhostAbility = "Ability_Never_Written";

        private const string Augment = "Augment_Sharpened";
        private const string OtherAugment = "Augment_Ornament";

        /// <summary>Two properties an augment copy carries its own number for. Any names will do —
        /// the slot and the file keep whatever the copy was minted with, and neither reads them.</summary>
        private const string Duration = "duration";
        private const string Chance = "chance";

        /// <summary>The section under test, as the file keys it.</summary>
        private const string BookSection = "abilityBook";

        [TestMethod]
        public void AnEmptySlotTakesAnAugmentAndGivesTheSameOneBack()
        {
            var socket = new AbilitySocket(SocketTwo, DexAbility, tier: 2);

            Assert.IsTrue(socket.IsEmpty);
            Assert.IsTrue(socket.Install(Copy(Augment)));
            Assert.IsFalse(socket.IsEmpty);
            Assert.AreEqual(Augment, socket.Extract()?.AugmentId);
            Assert.IsTrue(socket.IsEmpty, "extraction did not undo the install");
            Assert.IsNull(socket.Extract(), "a free slot handed something out");
        }

        [TestMethod]
        public void AnOccupiedSlotRefusesASecondAugmentInsteadOfSwallowingTheFirst()
        {
            var socket = new AbilitySocket(SocketTwo, DexAbility, tier: 2);
            socket.Install(Copy(Augment));

            Assert.IsFalse(socket.Install(Copy(OtherAugment)));
            Assert.AreEqual(Augment, socket.Augment?.AugmentId, "the occupant was replaced and the old one is gone");
        }

        [TestMethod]
        public void ATierTheGameHasNotUsedYetIsJustANumber()
        {
            // Nothing indexes by tier any more: a fourth slot is a socket carrying a 4, not a case
            // some switch has to have an arm for.
            var board = new AbilitySocketBoard();
            board.Sync([new AbilitySocketPlacement("socket_fourth", DexAbility, Tier: 4)]);

            Assert.IsTrue(board.Install("socket_fourth", Copy(Augment)));
            Assert.AreEqual(4, board.SocketsOf(DexAbility).Single().Tier);
            Assert.AreEqual(Augment, board.Extract("socket_fourth")?.AugmentId);
        }

        [TestMethod]
        public void TakingTheUnlockNodeOpensTheTierOneSlotWithTheAbility()
        {
            var board = new AbilitySocketBoard();
            IPassiveTreeService tree = NewTree();
            CreateService(tree, board);

            tree.Take(DexNode);

            AbilitySocket socket = board.SocketsOf(DexAbility).Single();
            Assert.AreEqual(1, socket.Tier, "the ability arrived without the slot that comes bundled with it");
            Assert.AreEqual(DexNode, socket.SocketId);
        }

        [TestMethod]
        public void TakingASocketNodeOpensItsSlot()
        {
            var board = new AbilitySocketBoard();
            IPassiveTreeService tree = NewTree();
            CreateService(tree, board);

            tree.Take(SocketThree);

            AbilitySocket socket = board.SocketsOf(DexAbility).Single();
            Assert.AreEqual(3, socket.Tier);
            Assert.AreEqual(SocketThree, socket.SocketId);
        }

        [TestMethod]
        public void GivingTheNodeBackClosesTheSlotAndTheAugmentGoesWithIt()
        {
            var board = new AbilitySocketBoard();
            IPassiveTreeService tree = NewTree();
            CreateService(tree, board);
            tree.Take(SocketTwo);
            Assert.IsTrue(board.Install(SocketTwo, Copy(Augment)), "the node opened no slot, so there is nothing to close");

            Assert.AreEqual(AllocationResult.Success, tree.Refund(SocketTwo));

            Assert.IsNull(board.Find(SocketTwo), "the slot outlived the point that paid for it");
            Assert.AreEqual(0, board.Sockets.Count);
        }

        [TestMethod]
        public void TwoNodesOfTheSameTierOnOneAbilityAreTwoSlots()
        {
            // The ornament: a second tier-2 slot on the same ability. The board is keyed by socket,
            // not by tier, so the second one does not evict the first.
            var board = new AbilitySocketBoard();
            IPassiveTreeService tree = NewTree();
            CreateService(tree, board);

            tree.Take(SocketTwo);
            tree.Take(SocketTwoOrnament);
            Assert.IsTrue(board.Install(SocketTwo, Copy(Augment)));
            Assert.IsTrue(board.Install(SocketTwoOrnament, Copy(OtherAugment)));

            CollectionAssert.AreEquivalent(
                new[] { Augment, OtherAugment },
                board.SocketsOf(DexAbility).Select(socket => socket.Augment?.AugmentId).ToArray(),
                "the second tier-2 augment displaced the first");
        }

        [TestMethod]
        public void ASocketNodePointingAtAnAbilityTheCatalogLostOpensNothing()
        {
            // A node of the same class that does name a known ability is taken first: without that
            // control the test would pass on a board nothing ever reaches.
            var board = new AbilitySocketBoard();
            IPassiveTreeService tree = NewTree();
            CreateService(tree, board);
            tree.Take(SocketTwo);
            Assert.IsNotNull(board.Find(SocketTwo), "no socket node opens a slot at all");

            Assert.AreEqual(AllocationResult.Success, tree.Take(GhostSocket));

            Assert.IsNull(board.Find(GhostSocket), "a slot opened on an ability no catalog holds");
            Assert.AreEqual(1, board.Sockets.Count);
        }

        [TestMethod]
        public void RespecLeavesNoSlots()
        {
            var board = new AbilitySocketBoard();
            IPassiveTreeService tree = NewTree();
            CreateService(tree, board);
            tree.Take(DexNode);
            tree.Take(SocketTwo);
            Assert.IsTrue(board.Install(SocketTwo, Copy(Augment)), "the respec is handed a board that was already empty");
            Assert.AreEqual(2, board.Sockets.Count, "the unlock node and the socket node opened a slot each");

            tree.Respec();

            Assert.AreEqual(0, board.Sockets.Count);
        }

        [TestMethod]
        public void ANewPlaythroughStartsWithNoSlots()
        {
            var board = new AbilitySocketBoard();
            var tree = NewTree();
            CreateService(tree, board);
            tree.Take(SocketTwo);
            Assert.IsTrue(board.Install(SocketTwo, Copy(Augment)), "the playthrough being left behind never had a slot");

            tree.ResetSession();

            Assert.AreEqual(0, board.Sockets.Count, "the previous playthrough's slot survived the reset");
        }

        [TestMethod]
        public void ANodeRepointedAtAnotherAbilityRebuildsItsSlot()
        {
            // The allocation hands the board a slot of that id pointing somewhere else: the id still
            // names a node, but not the same slot. The augment was chosen for the ability the node
            // used to carry, so the slot is rebuilt empty rather than re-labelled around it.
            var board = new AbilitySocketBoard();
            board.Sync([new AbilitySocketPlacement(SocketTwo, DexAbility, Tier: 2)]);
            board.Install(SocketTwo, Copy(Augment));

            board.Sync([new AbilitySocketPlacement(SocketTwo, DexAbilityTwo, Tier: 2)]);

            Assert.AreEqual(DexAbilityTwo, board.Find(SocketTwo)?.AbilityId);
            Assert.IsTrue(board.Find(SocketTwo)?.IsEmpty, "the augment followed the node onto another ability");
        }

        [TestMethod]
        public void AnAugmentDoesNotFollowItsNodeOntoAnotherAbilityAcrossASaveAndLoad()
        {
            // The same drift a build apart, on the road it actually travels: the file names the slot by
            // id, and in the build reading it that id belongs to another ability. The entry carries the
            // slot it was chosen for, so the load drops it instead of putting it into the rebuilt slot.
            SaveFile file = Save([SocketTwo], (SocketTwo, Copy(Augment)));

            var board = new AbilitySocketBoard();
            IPassiveTreeService tree = NewTree(socketTwoAbility: DexAbilityTwo);
            CreateService(tree, board);
            ManagerFor(tree, board).Restore(file);

            Assert.AreEqual(DexAbilityTwo, board.Find(SocketTwo)?.AbilityId, "the load did not rebuild the slot");
            Assert.IsTrue(board.Find(SocketTwo)?.IsEmpty, "the augment followed the node onto another ability");
        }

        [TestMethod]
        public void ATreeThatFailedToLoadKeepsTheAugmentsInTheFile()
        {
            // A document with no content is what the reader hands out for a file that failed to parse:
            // no node can be found, so no slot opens and nothing can be installed. The file's augments
            // are held rather than spent â€” one bad launch costs the player the tree's content, not what
            // he put into it.
            SaveFile file = Save([SocketTwo], (SocketTwo, Copy(Augment)));

            var board = new AbilitySocketBoard();
            IPassiveTreeService unreadable = NewService(new PassiveTreeDocument());
            CreateService(unreadable, board);
            ManagerFor(unreadable, board).Restore(file);

            Assert.AreEqual(0, board.Sockets.Count, "a document with no nodes opened a slot anyway");
            JToken rewritten = new AbilityBookSaveParticipant(AccessorFor(NewBook()), board).Capture();
            Assert.AreEqual(Augment, rewritten["sockets"]?[SocketTwo]?["augment"]?.Value<string>(),
                "the next save wrote the slot empty and the augment is gone for good");
        }

        [TestMethod]
        public void TheFirstAllocationThatOpensNoSlotDropsWhatWasHeldForIt()
        {
            // The holding is a postponement, not a promise. The first allocation the board can read
            // says the character owns no slot at all â€” that is the answer the entry was waiting for,
            // and it is dropped rather than carried into every save that follows.
            var board = new AbilitySocketBoard();
            board.Restore([Occupant(SocketTwo, DexAbility, tier: 2)]);
            Assert.AreEqual(1, board.Occupants.Count, "the entry was never held, so nothing is being answered");

            board.Sync([]);

            Assert.AreEqual(0, board.Occupants.Count, "the augment outlived the allocation that had no slot for it");
        }

        [TestMethod]
        public void TheFirstAllocationSeatsWhatWasHeldForTheSlotItOpens()
        {
            // The tree came back: the slot the file named is open again and still the one the augment
            // was chosen for. What was held goes into it â€” a load that survived a broken launch must
            // end with the augment working, not merely with it remembered.
            var board = new AbilitySocketBoard();
            board.Restore([Occupant(SocketTwo, DexAbility, tier: 2)]);

            board.Sync([new AbilitySocketPlacement(SocketTwo, DexAbility, Tier: 2)]);

            Assert.AreEqual(Augment, board.Find(SocketTwo)?.Augment?.AugmentId,
                "the slot opened empty and the held augment stayed outside it");
            Assert.AreEqual(1, board.Occupants.Count, "the augment is counted both in the slot and outside it");
        }

        [TestMethod]
        public void AHeldEntryTheAllocationRefusedCannotOverwriteWhatThePlayerInstalls()
        {
            // The slot came back pointing at another ability, so the held augment is not its. The
            // player fills the rebuilt slot himself, and that is what the file has to record: an entry
            // left outside the sockets carries the same socket Id and would take the key with it.
            var board = new AbilitySocketBoard();
            board.Restore([Occupant(SocketTwo, DexAbility, tier: 2)]);
            board.Sync([new AbilitySocketPlacement(SocketTwo, DexAbilityTwo, Tier: 2)]);

            Assert.IsTrue(board.Install(SocketTwo, Copy(OtherAugment)), "the rebuilt slot would not take an augment at all");

            JToken written = new AbilityBookSaveParticipant(AccessorFor(NewBook()), board).Capture();
            Assert.AreEqual(OtherAugment, written["sockets"]?[SocketTwo]?["augment"]?.Value<string>(),
                "the file kept the held augment instead of the one the player put in the slot");
        }

        [TestMethod]
        public void ANewPlaythroughDoesNotInheritAugmentsHeldForATreeThatNeverLoaded()
        {
            // The one road the fix above cannot cover: the document never parsed, so no allocation is
            // ever read and nothing judges the held entries. Starting a new game is the answer â€” the
            // board is a singleton, and the previous playthrough's augment would otherwise be written
            // into the first save of the next one. No stand-in for the wiring: the reset has to reach
            // the board through the container the game builds.
            SaveFile file = Save([SocketTwo], (SocketTwo, Copy(Augment)));

            var board = new AbilitySocketBoard();
            IPassiveTreeService unreadable = NewService(new PassiveTreeDocument());
            CreateService(unreadable, board);
            ManagerFor(unreadable, board).Restore(file);
            Assert.AreEqual(1, board.Occupants.Count, "the augment was never held, so there is nothing to leak");

            NewSessionOver(board).ResetSession();

            Assert.AreEqual(0, board.Occupants.Count, "the fresh playthrough owns the previous one's augment");
        }

        [TestMethod]
        public void TwoAugmentsOfOneTierSurviveASaveAndLoad()
        {
            SaveFile file = Save([DexNode, SocketTwo, SocketTwoOrnament], (SocketTwo, Copy(Augment)), (SocketTwoOrnament, Copy(OtherAugment)));

            var targetBoard = new AbilitySocketBoard();
            IPassiveTreeService targetTree = NewTree();
            CreateService(targetTree, targetBoard);
            ManagerFor(targetTree, targetBoard).Restore(file);

            Assert.AreEqual(Augment, targetBoard.Find(SocketTwo)?.Augment?.AugmentId);
            Assert.AreEqual(OtherAugment, targetBoard.Find(SocketTwoOrnament)?.Augment?.AugmentId);
        }

        [TestMethod]
        public void TheFileCannotPutAnAugmentIntoASlotNoNodeOpens()
        {
            // The file names a slot the restored allocation does not open. The slots are the
            // allocation's to give, so the entry is dropped rather than conjuring one back.
            var board = new AbilitySocketBoard();
            IPassiveTreeService tree = NewTree();
            CreateService(tree, board);
            var data = new AbilityBookSaveData { CurrentStance = Stance.Dexterity };
            data.Sockets[SocketTwo] = new SocketSaveData { Augment = Augment, Ability = DexAbility, Tier = 2 };
            var participant = new AbilityBookSaveParticipant(AccessorFor(NewBook()), board);

            participant.Restore(JToken.FromObject(data), participant.Version);

            Assert.AreEqual(0, board.Sockets.Count);
            Assert.IsNull(board.Find(SocketTwo));
        }

        [TestMethod]
        public void AFileWithoutTheSectionEmptiesTheSlots()
        {
            // The board is a singleton: starting a game whose file carries no ability book at all must
            // not leave the augments of the playthrough before sitting in the slots.
            var board = new AbilitySocketBoard();
            board.Sync([new AbilitySocketPlacement(SocketTwo, DexAbility, Tier: 2)]);
            board.Install(SocketTwo, Copy(Augment));

            new AbilityBookSaveParticipant(AccessorFor(NewBook()), board).RestoreWithoutSection();

            Assert.IsTrue(board.Find(SocketTwo)?.IsEmpty == true);
        }

        [TestMethod]
        public void ACopyKeepsTheNumbersItRolledAcrossASaveAndLoad()
        {
            // The whole point of the section moving version: an augment is a copy with numbers of its
            // own, and nothing in the game can roll the same ones twice. A load that brought the id
            // back and left the numbers behind would hand the player a different augment than the one
            // he put in the slot — and one he cannot tell apart from it until he reads the tooltip.
            AugmentInstance rolled = Copy(Augment, (Duration, 4f), (Chance, 0.185f));
            SaveFile file = Save([DexNode, SocketTwo], (SocketTwo, rolled));

            var board = new AbilitySocketBoard();
            IPassiveTreeService tree = NewTree();
            CreateService(tree, board);
            ManagerFor(tree, board).Restore(file);

            AugmentInstance? restored = board.Find(SocketTwo)?.Augment;
            Assert.AreEqual(Augment, restored?.AugmentId, "the slot came back empty");
            Assert.AreEqual(4f, restored?.Values[Duration], "the copy came back at another number");
            Assert.AreEqual(0.185f, restored?.Values[Chance], "the copy came back at another number");
        }

        [TestMethod]
        public void AFileWrittenBeforeTheCopiesIsRefusedWholeAndLeavesTheOtherSectionsAlone()
        {
            // A version 3 entry names a record and no copy of it. Finishing the read would mean either
            // inventing numbers the player never rolled or quietly handing him the record's bases, so
            // the section is refused instead — and refused by itself, without an exception: the rest of
            // the file is a playthrough this build reads perfectly well, and the allocation beside the
            // book is what proves it landed.
            SaveFile file = Save([DexNode, SocketTwo], (SocketTwo, Copy(Augment)));
            file.Sections[BookSection] = new SaveSection { Version = 3, Data = file.Sections[BookSection].Data };

            var board = new AbilitySocketBoard();
            IPassiveTreeService tree = NewTree();
            CreateService(tree, board);
            ManagerFor(tree, board).Restore(file);

            Assert.IsTrue(board.Find(SocketTwo)?.IsEmpty == true, "a slot came back filled out of a file that carries no copies");
            Assert.AreEqual(0, board.Occupants.Count, "the refused entry is being held outside the slots");
            CollectionAssert.Contains(tree.TakenNodes.ToArray(), SocketTwo,
                "the refused book took the allocation down with it");
        }

        [TestMethod]
        public void TheSectionDeclaresTheVersionThatCarriesTheRolledNumbers()
        {
            Assert.AreEqual(4, new AbilityBookSaveParticipant(AccessorFor(NewBook())).Version);
        }

        /// <summary>A file holding an allocation and the augments installed into the slots it opened.</summary>
        private static SaveFile Save(string[] taken, params (string Socket, AugmentInstance Augment)[] installed)
        {
            var board = new AbilitySocketBoard();
            IPassiveTreeService tree = NewTree();
            CreateService(tree, board);
            foreach (string nodeId in taken) tree.Take(nodeId);
            foreach ((string socket, AugmentInstance augment) in installed)
                Assert.IsTrue(board.Install(socket, augment), $"the fixture could not fill {socket}");

            return ManagerFor(tree, board).Capture(new SaveMetadata());
        }

        /// <summary>One saved augment together with the slot it was written under.</summary>
        private static AbilitySocketOccupant Occupant(string socketId, string abilityId, int tier) =>
            new(new AbilitySocketPlacement(socketId, abilityId, tier), Copy(Augment));

        /// <summary>The reset stack the game builds, holding nothing but the board.</summary>
        private static ISessionResetService NewSessionOver(IAbilitySocketBoard board)
        {
            var services = new ServiceCollection();
            services.AddSingleton<LoadScope>();
            services.AddSingleton(board);
            services.AddSessionReset();
            return services.BuildServiceProvider().GetRequiredService<ISessionResetService>();
        }

        private static ISaveManager ManagerFor(IPassiveTreeService tree, IAbilitySocketBoard board)
        {
            var manager = new SaveManager(new LoadScope());
            manager.Register(new PassiveTreeSaveParticipant(tree));
            manager.Register(new AbilityBookSaveParticipant(AccessorFor(NewBook()), board));
            return manager;
        }

        private static AbilityBookComponent NewBook() => new(new Mock<IFightable>().Object);

        private static IPlayerAccessor AccessorFor(IAbilityBookComponent book)
        {
            var player = new Mock<IPlayer>();
            player.SetupGet(p => p.AbilityBook).Returns(book);
            var accessor = new PlayerAccessor();
            accessor.Set(player.Object);
            return accessor;
        }

        /// <summary>One unlock node and three socket nodes hanging off the seed: two of them are
        /// tier 2 on the same ability (the ornament), one is tier 2 on an ability no catalog holds.</summary>
        /// <param name="socketTwoAbility">Which ability the plain tier-2 node carries. A second build
        /// of the same tree is one that points that node somewhere else.</param>
        private static PassiveTreeService NewTree(string socketTwoAbility = DexAbility)
        {
            var document = new PassiveTreeDocument();
            document.AddNode(new PassiveNode { Id = Seed, Kind = PassiveNodeKind.Start, Stance = Stance.Strength });

            Add(DexNode, PassiveNodeKind.AbilityUnlock, DexAbility);
            Add(SocketTwo, PassiveNodeKind.SocketTier2, socketTwoAbility);
            Add(SocketTwoOrnament, PassiveNodeKind.SocketTier2, DexAbility);
            Add(SocketThree, PassiveNodeKind.SocketTier3, DexAbility);
            Add(GhostSocket, PassiveNodeKind.SocketTier2, GhostAbility);

            return NewService(document);

            void Add(string id, PassiveNodeKind kind, string abilityId)
            {
                document.AddNode(new PassiveNode { Id = id, Kind = kind, AbilityId = abilityId });
                document.Link(Seed, id);
            }
        }

        /// <summary>The allocation service over one document, with a point for every node in it.</summary>
        private static PassiveTreeService NewService(PassiveTreeDocument document)
        {
            var service = new PassiveTreeService(new SocketTreeProviderStub(document), ConditionCatalogs.Empty());
            service.SetTotalPoints(document.Nodes.Count);
            return service;
        }

        private static AbilityUnlockService CreateService(IPassiveTreeService tree, IAbilitySocketBoard board) =>
            new(AccessorFor(NewBook()), new SocketAbilityProvider(), tree, board);

        /// <summary>Catalog stub: two dexterity abilities, nothing hidden.</summary>
        private sealed class SocketAbilityProvider : IAbilityProvider
        {
            public IReadOnlyCollection<string> KnownAbilityIds => [DexAbility, DexAbilityTwo];

            public IAbility CreateAbility(string abilityId)
            {
                string instanceId = Guid.NewGuid().ToString();
                var ability = new Mock<IAbility>();
                ability.SetupGet(a => a.Id).Returns(abilityId);
                ability.SetupGet(a => a.InstanceId).Returns(instanceId);
                ability.SetupGet(a => a.CurrentUpgrades).Returns(new Dictionary<int, IAbilityUpgrade>());
                ability.Setup(a => a.IsSame(It.IsAny<string>())).Returns((string other) => other == instanceId);
                return ability.Object;
            }

            public Stance GetAbilityStance(string abilityId) => Stance.Dexterity;

            public bool IsHidden(string abilityId) => false;
        }

        /// <summary>Stands in for the data pipeline: the document is already parsed.</summary>
        private sealed class SocketTreeProviderStub(PassiveTreeDocument tree) : IPassiveTreeProvider
        {
            public PassiveTreeDocument Tree { get; } = tree;

            public IReadOnlyList<string> Issues => [];
        }
    }
}

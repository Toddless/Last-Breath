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
    using static AbilityBookStand;
    using static AugmentCopies;

    /// <summary>
    /// An augment slot works exactly while the node that opened it is taken, and what sits in it is the
    /// only part of the arrangement the save file remembers. A slot is addressed by its whole signature
    /// rather than by the node alone, so an ability can carry two slots of the same tier and a node
    /// repointed between builds does not make one slot out of two.
    ///
    /// Giving the node back does not spend what is in the slot. The augment is the player's property:
    /// the slot stops working, stays on the board as a place he can take it OUT of and takes nothing
    /// in, and disappears the moment it is empty.
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

        /// <summary>The section the allocation is written under.</summary>
        private const string TreeSection = "passiveTree";

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
        public void AClosedSlotTakesNothingAndStillGivesBackWhatIsInIt()
        {
            // The rule at the bottom of the stack, where every road has to obey it — a load included.
            // The empty case is the one the guard is actually for: a full slot would be refused for
            // being full, and the load builds its remove-only slots by filling them BEFORE closing
            // them precisely because a closed one takes nothing.
            var empty = new AbilitySocket(SocketTwo, DexAbility, tier: 2);
            empty.Close();
            Assert.IsFalse(empty.Install(Copy(Augment)), "an empty slot with no node behind it took an augment");
            Assert.IsTrue(empty.IsEmpty);

            var socket = new AbilitySocket(SocketTwo, DexAbility, tier: 2);
            socket.Install(Copy(Augment));
            socket.Close();

            Assert.IsFalse(socket.Install(Copy(OtherAugment)), "a slot with no node behind it took a second augment");
            Assert.IsNull(socket.WorkingAugment, "the ability goes on wearing an augment the allocation stopped paying for");
            Assert.AreEqual(Augment, socket.Augment?.AugmentId, "the player's augment is not even in the slot any more");
            Assert.AreEqual(Augment, socket.Extract()?.AugmentId, "the only way out of a closed slot is shut");
        }

        [TestMethod]
        public void ATierTheGameHasNotUsedYetIsJustANumber()
        {
            // Nothing indexes by tier any more: a fourth slot is a socket carrying a 4, not a case
            // some switch has to have an arm for.
            var board = new AbilitySocketBoard();
            board.Sync([new AbilitySocketPlacement("socket_fourth", DexAbility, Tier: 4)]);

            Assert.IsTrue(board.Install(board.At("socket_fourth"), Copy(Augment)));
            Assert.AreEqual(4, board.SocketsOf(DexAbility).Single().Tier);
            Assert.AreEqual(Augment, board.Extract(board.At("socket_fourth"))?.AugmentId);
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
        public void GivingTheNodeBackDropsAnEmptySlot()
        {
            var board = new AbilitySocketBoard();
            IPassiveTreeService tree = NewTree();
            CreateService(tree, board);
            tree.Take(SocketTwo);
            Assert.IsNotNull(board.Find(board.At(SocketTwo)), "the node opened no slot, so there is nothing to close");

            Assert.AreEqual(AllocationResult.Success, tree.Refund(SocketTwo));

            Assert.AreEqual(0, board.Sockets.Count, "an empty slot outlived the point that paid for it");
        }

        [TestMethod]
        public void GivingTheNodeBackKeepsTheAugmentAndLeavesTheSlotRemoveOnly()
        {
            // The whole point of a closed slot. A refund is the player changing his mind about a NODE;
            // the augment in it he found, bought or rolled, and nothing in the game can draw its
            // numbers again — so the slot stays as the way back out of itself.
            var board = new AbilitySocketBoard();
            IPassiveTreeService tree = NewTree();
            CreateService(tree, board);
            tree.Take(SocketTwo);
            string slot = board.At(SocketTwo);
            Assert.IsTrue(board.Install(slot, Copy(Augment)), "the node opened no slot, so there is nothing to close");

            Assert.AreEqual(AllocationResult.Success, tree.Refund(SocketTwo));

            AbilitySocket? closed = board.Find(slot);
            Assert.IsNotNull(closed, "the refund destroyed the slot and the player's augment with it");
            Assert.IsFalse(closed.IsOpen, "the refunded node goes on backing its slot");
            Assert.AreEqual(Augment, closed.Augment?.AugmentId, "the augment went with the node");
            Assert.IsNull(closed.WorkingAugment, "the ability goes on wearing an augment no node pays for");
        }

        [TestMethod]
        public void AClosedSlotRefusesAnInstallThroughTheBoardToo()
        {
            var board = new AbilitySocketBoard();
            IPassiveTreeService tree = NewTree();
            CreateService(tree, board);
            tree.Take(SocketTwo);
            string slot = board.At(SocketTwo);
            board.Install(slot, Copy(Augment));
            tree.Refund(SocketTwo);

            Assert.IsFalse(board.Install(slot, Copy(OtherAugment)), "a slot no node backs took a second augment");
            Assert.AreEqual(Augment, board.Find(slot)?.Augment?.AugmentId, "the refused install moved the first augment");
        }

        [TestMethod]
        public void TakingTheLastAugmentOutOfAClosedSlotTakesTheSlotWithIt()
        {
            // What ends the remove-only row in a window: the slot existed only to give the augment
            // back, and it has.
            var board = new AbilitySocketBoard();
            IPassiveTreeService tree = NewTree();
            CreateService(tree, board);
            tree.Take(SocketTwo);
            string slot = board.At(SocketTwo);
            board.Install(slot, Copy(Augment));
            tree.Refund(SocketTwo);

            Assert.AreEqual(Augment, board.Extract(slot)?.AugmentId);

            Assert.IsNull(board.Find(slot), "the emptied closed slot is still on the board");
            Assert.AreEqual(0, board.Sockets.Count);
        }

        [TestMethod]
        public void BuyingTheNodeBackWakesTheSlotUpAroundWhatIsInIt()
        {
            // The same signature is the same slot: the player paid for exactly the place his augment
            // was sitting in, and a second empty slot beside it would be nonsense.
            var board = new AbilitySocketBoard();
            IPassiveTreeService tree = NewTree();
            CreateService(tree, board);
            tree.Take(SocketTwo);
            string slot = board.At(SocketTwo);
            board.Install(slot, Copy(Augment));
            tree.Refund(SocketTwo);

            Assert.AreEqual(AllocationResult.Success, tree.Take(SocketTwo));

            Assert.AreEqual(1, board.Sockets.Count, "buying the node back opened a second slot beside the closed one");
            Assert.IsTrue(board.Find(slot)?.IsOpen == true);
            Assert.AreEqual(Augment, board.Find(slot)?.WorkingAugment?.AugmentId, "the augment stayed asleep in a slot that works again");
        }

        [TestMethod]
        public void TwoNodesOfTheSameTierOnOneAbilityAreTwoSlots()
        {
            // The ornament: a second tier-2 slot on the same ability. The board is keyed by the whole
            // signature, not by tier, so the second one does not evict the first.
            var board = new AbilitySocketBoard();
            IPassiveTreeService tree = NewTree();
            CreateService(tree, board);

            tree.Take(SocketTwo);
            tree.Take(SocketTwoOrnament);
            Assert.IsTrue(board.Install(board.At(SocketTwo), Copy(Augment)));
            Assert.IsTrue(board.Install(board.At(SocketTwoOrnament), Copy(OtherAugment)));

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
            Assert.IsNotNull(board.Find(board.At(SocketTwo)), "no socket node opens a slot at all");

            Assert.AreEqual(AllocationResult.Success, tree.Take(GhostSocket));

            Assert.IsNull(board.Find(board.At(GhostSocket)), "a slot opened on an ability no catalog holds");
            Assert.AreEqual(1, board.Sockets.Count);
        }

        [TestMethod]
        public void ARespecKeepsOnlyTheSlotsThatHoldSomething()
        {
            var board = new AbilitySocketBoard();
            IPassiveTreeService tree = NewTree();
            CreateService(tree, board);
            tree.Take(DexNode);
            tree.Take(SocketTwo);
            string filled = board.At(SocketTwo);
            Assert.IsTrue(board.Install(filled, Copy(Augment)), "the respec is handed a board that was already empty");
            Assert.AreEqual(2, board.Sockets.Count, "the unlock node and the socket node opened a slot each");

            tree.Respec();

            Assert.AreEqual(1, board.Sockets.Count, "the empty slot survived the respec, or the filled one did not");
            Assert.AreEqual(Augment, board.Find(filled)?.Augment?.AugmentId, "the respec spent the player's augment");
            Assert.IsFalse(board.Find(filled)?.IsOpen);
        }

        [TestMethod]
        public void ANewPlaythroughStartsWithNoSlots()
        {
            var board = new AbilitySocketBoard();
            var tree = NewTree();
            CreateService(tree, board);
            tree.Take(SocketTwo);
            Assert.IsTrue(board.Install(board.At(SocketTwo), Copy(Augment)), "the playthrough being left behind never had a slot");

            tree.ResetSession();
            Assert.AreEqual(1, board.Sockets.Count, "emptying the allocation spent the augment instead of closing its slot");

            NewSessionOver(board).ResetSession();

            Assert.AreEqual(0, board.Sockets.Count, "the previous playthrough's slot survived the reset");
        }

        [TestMethod]
        public void ANodeRepointedAtAnotherAbilityLeavesTheOldSlotBesideTheNewOne()
        {
            // The allocation hands the board a slot of that id pointing somewhere else: the id still
            // names a node, but not the same slot. The augment was chosen for the ability the node used
            // to carry, so it stays where it is, remove-only — and the slot the player paid for opens
            // empty beside it. Both are his.
            var board = new AbilitySocketBoard();
            board.Sync([new AbilitySocketPlacement(SocketTwo, DexAbility, Tier: 2)]);
            string old = board.At(SocketTwo);
            board.Install(old, Copy(Augment));

            board.Sync([new AbilitySocketPlacement(SocketTwo, DexAbilityTwo, Tier: 2)]);

            string fresh = Address(SocketTwo, DexAbilityTwo, 2);
            Assert.IsTrue(board.Find(fresh)?.IsEmpty, "the augment followed the node onto another ability");
            Assert.IsTrue(board.Find(fresh)?.IsOpen, "the slot the node opens now is not live");
            Assert.AreEqual(Augment, board.Find(old)?.Augment?.AugmentId, "the augment was spent by a node moving under it");
            Assert.IsFalse(board.Find(old)?.IsOpen);
        }

        [TestMethod]
        public void AnAugmentDoesNotFollowItsNodeOntoAnotherAbilityAcrossASaveAndLoad()
        {
            // The same drift a build apart, on the road it actually travels: the file names the slot by
            // its signature, and in the build reading it that node belongs to another ability. The
            // augment does not move onto it — and is not thrown away either.
            SaveFile file = Save([SocketTwo], (SocketTwo, Copy(Augment)));

            var board = new AbilitySocketBoard();
            IPassiveTreeService tree = NewTree(socketTwoAbility: DexAbilityTwo);
            CreateService(tree, board);
            ManagerFor(tree, board).Restore(file);

            string fresh = Address(SocketTwo, DexAbilityTwo, 2);
            Assert.IsTrue(board.Find(fresh)?.IsEmpty, "the augment followed the node onto another ability");
            Assert.AreEqual(Augment, board.Find(Address(SocketTwo, DexAbility, 2))?.Augment?.AugmentId,
                "the load spent an augment the player owns");
        }

        [TestMethod]
        public void ATreeThatFailedToLoadKeepsTheAugmentsInTheFile()
        {
            // A document with no content is what the reader hands out for a file that failed to parse:
            // no node can be found, so no slot opens. The file's augments come back as closed slots and
            // are written out whole — one bad launch costs the player the tree's content, not what he
            // put into it.
            SaveFile file = Save([SocketTwo], (SocketTwo, Copy(Augment)));

            var board = new AbilitySocketBoard();
            IPassiveTreeService unreadable = NewService(new PassiveTreeDocument());
            CreateService(unreadable, board);
            ManagerFor(unreadable, board).Restore(file);

            Assert.AreEqual(1, board.Sockets.Count, "the file's slot was neither opened nor kept");
            Assert.IsFalse(board.Sockets.Single().IsOpen, "a document with no nodes opened a live slot anyway");
            JToken rewritten = new AbilityBookSaveParticipant(AccessorFor(NewBook()), board).Capture();
            Assert.AreEqual(Augment, WrittenSockets(rewritten).Single()["augment"]?.Value<string>(),
                "the next save wrote the slot empty and the augment is gone for good");
        }

        [TestMethod]
        public void TheFirstAllocationThatOpensNoSlotKeepsWhatWasHeldForIt()
        {
            // The old board dropped such an entry at the first allocation that could not place it.
            // Nothing about an allocation makes the player's augment somebody else's: the slot stays,
            // remove-only, and he decides what becomes of what is in it.
            var board = new AbilitySocketBoard();
            board.Restore([Occupant(SocketTwo, DexAbility, tier: 2)], []);
            Assert.AreEqual(1, board.Occupants.Count, "the entry was never restored, so nothing is being answered");

            board.Sync([]);

            Assert.AreEqual(1, board.Occupants.Count, "the augment was spent by an allocation that had no slot for it");
            Assert.IsFalse(board.Sockets.Single().IsOpen);
        }

        [TestMethod]
        public void TheFirstAllocationWakesUpWhatWasHeldForTheSlotItOpens()
        {
            // The tree came back: the slot the file named is open again and still the one the augment
            // was chosen for. It starts working — a load that survived a broken launch must end with
            // the augment doing something, not merely with it remembered.
            var board = new AbilitySocketBoard();
            board.Restore([Occupant(SocketTwo, DexAbility, tier: 2)], []);

            board.Sync([new AbilitySocketPlacement(SocketTwo, DexAbility, Tier: 2)]);

            AbilitySocket? slot = board.Find(Address(SocketTwo, DexAbility, 2));
            Assert.AreEqual(Augment, slot?.WorkingAugment?.AugmentId, "the slot opened empty, or opened without waking up");
            Assert.AreEqual(1, board.Occupants.Count, "the augment is counted twice");
            Assert.AreEqual(1, board.Sockets.Count, "a second slot appeared beside the one that woke up");
        }

        [TestMethod]
        public void ASlotHeldFromTheFileAndTheOneTheNodeOpensNowAreBothWritten()
        {
            // The case a map keyed by node could not express: one node, two slots, an augment in each.
            // Written under one key, one of the two would be gone from the file for good.
            var board = new AbilitySocketBoard();
            board.Restore([Occupant(SocketTwo, DexAbility, tier: 2)], []);
            board.Sync([new AbilitySocketPlacement(SocketTwo, DexAbilityTwo, Tier: 2)]);

            Assert.IsTrue(board.Install(Address(SocketTwo, DexAbilityTwo, 2), Copy(OtherAugment)),
                "the slot the node opens now would not take an augment at all");

            JToken written = new AbilityBookSaveParticipant(AccessorFor(NewBook()), board).Capture();
            CollectionAssert.AreEquivalent(
                new[] { Augment, OtherAugment },
                WrittenSockets(written).Select(entry => entry["augment"]?.Value<string>()).ToArray(),
                "one of the two augments the character owns is not in the file");
        }

        [TestMethod]
        public void ANewPlaythroughDoesNotInheritAugmentsHeldForATreeThatNeverLoaded()
        {
            // The one road the rule above cannot cover: the document never parsed, so no allocation is
            // ever read and every entry of the file came back closed. Starting a new game is the answer
            // — the board is a singleton, and the previous playthrough's augment would otherwise be
            // written into the first save of the next one. No stand-in for the wiring: the reset has to
            // reach the board through the container the game builds.
            SaveFile file = Save([SocketTwo], (SocketTwo, Copy(Augment)));

            var board = new AbilitySocketBoard();
            IPassiveTreeService unreadable = NewService(new PassiveTreeDocument());
            CreateService(unreadable, board);
            ManagerFor(unreadable, board).Restore(file);
            Assert.AreEqual(1, board.Occupants.Count, "the augment was never kept, so there is nothing to leak");

            NewSessionOver(board).ResetSession();

            Assert.AreEqual(0, board.Occupants.Count, "the fresh playthrough owns the previous one's augment");
        }

        [TestMethod]
        public void ALoadDoesNotInheritTheClosedSlotsOfThePlaythroughBefore()
        {
            // The board outlives the character. A slot closed by one playthrough's refund must not turn
            // up in the next one's window holding an augment its owner never had.
            var board = new AbilitySocketBoard();
            board.Sync([new AbilitySocketPlacement(SocketTwo, DexAbility, Tier: 2)]);
            board.Install(board.At(SocketTwo), Copy(Augment));
            board.Sync([]);
            Assert.AreEqual(1, board.Sockets.Count, "there is no closed slot to leak");

            board.Restore([], []);

            Assert.AreEqual(0, board.Sockets.Count, "the previous character's closed slot survived the load");
        }

        [TestMethod]
        public void ARemoveOnlySlotSurvivesASaveAndLoad()
        {
            // Without this the rule is a lie the moment the player quits: he would come back to a
            // character whose augment is in no slot and in no bag.
            SaveFile file = Save([SocketTwo], (SocketTwo, Copy(Augment, (Duration, 3f))));

            // The next build never opens that node's slot at all: the file is loaded against a tree
            // whose points were never spent.
            var board = new AbilitySocketBoard();
            IPassiveTreeService tree = NewTree();
            CreateService(tree, board);
            ManagerFor(tree, board).Restore(StrippedAllocation(file));

            AbilitySocket? slot = board.Find(Address(SocketTwo, DexAbility, 2));
            Assert.IsNotNull(slot, "the slot no node opens did not come back and the augment is gone");
            Assert.IsFalse(slot.IsOpen);
            Assert.AreEqual(3f, slot.Augment?.Values[Duration], "the copy came back at another number");
        }

        [TestMethod]
        public void TwoAugmentsOfOneTierSurviveASaveAndLoad()
        {
            SaveFile file = Save([DexNode, SocketTwo, SocketTwoOrnament], (SocketTwo, Copy(Augment)), (SocketTwoOrnament, Copy(OtherAugment)));

            var targetBoard = new AbilitySocketBoard();
            IPassiveTreeService targetTree = NewTree();
            CreateService(targetTree, targetBoard);
            ManagerFor(targetTree, targetBoard).Restore(file);

            Assert.AreEqual(Augment, targetBoard.Find(targetBoard.At(SocketTwo))?.Augment?.AugmentId);
            Assert.AreEqual(OtherAugment, targetBoard.Find(targetBoard.At(SocketTwoOrnament))?.Augment?.AugmentId);
        }

        [TestMethod]
        public void AFileNamingASlotNoNodeOpensBringsItBackRemoveOnly()
        {
            // The file names a slot the restored allocation does not open. The slot is not conjured
            // back into service — it comes back closed, so the augment can be taken out and nothing
            // can be put in.
            var board = new AbilitySocketBoard();
            IPassiveTreeService tree = NewTree();
            CreateService(tree, board);
            var data = new AbilityBookSaveData { CurrentStance = Stance.Dexterity };
            data.Sockets.Add(new SocketSaveData { Socket = SocketTwo, Augment = Augment, Ability = DexAbility, Tier = 2 });
            var participant = new AbilityBookSaveParticipant(AccessorFor(NewBook()), board, copies: LegacyDraw(Rarity.Rare));

            participant.Restore(JToken.FromObject(data), participant.Version);

            AbilitySocket? slot = board.Find(Address(SocketTwo, DexAbility, 2));
            Assert.IsNotNull(slot);
            Assert.IsFalse(slot.IsOpen);
            Assert.AreEqual(Augment, slot.Augment?.AugmentId);
        }

        [TestMethod]
        public void AFileWithoutTheSectionEmptiesTheSlots()
        {
            // The board is a singleton: starting a game whose file carries no ability book at all must
            // not leave the augments of the playthrough before sitting in the slots.
            var board = new AbilitySocketBoard();
            board.Sync([new AbilitySocketPlacement(SocketTwo, DexAbility, Tier: 2)]);
            board.Install(board.At(SocketTwo), Copy(Augment));

            new AbilityBookSaveParticipant(AccessorFor(NewBook()), board).RestoreWithoutSection();

            Assert.IsTrue(board.Find(board.At(SocketTwo))?.IsEmpty == true);
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

            AugmentInstance? restored = board.Find(board.At(SocketTwo))?.Augment;
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

            Assert.IsTrue(board.Find(board.At(SocketTwo))?.IsEmpty == true, "a slot came back filled out of a file that carries no copies");
            Assert.AreEqual(0, board.Occupants.Count, "the refused entry is being held anyway");
            CollectionAssert.Contains(tree.TakenNodes.ToArray(), SocketTwo,
                "the refused book took the allocation down with it");
        }

        [TestMethod]
        public void TheSectionDeclaresAVersionWhereOneNodeMayCarryTwoSlots()
        {
            // Six was where the socket map became a list; seven added the ornaments beside it. The
            // assertion is a floor rather than an equality — what it is about is the shape of the socket
            // entries, and that shape has not moved since (OrnamentTests names the current number).
            Assert.IsTrue(new AbilityBookSaveParticipant(AccessorFor(NewBook())).Version >= 6);
        }

        [TestMethod]
        public void AFileWrittenWhenTheSocketsWereKeyedByNodeIsReadForItsSlots()
        {
            // Version 5 wrote the entries as a map with the node id for a key. Every field version 6
            // needs is in it, so the file is migrated rather than refused — refusing it would take the
            // player's augments with the shape of the property.
            // The rarity assertion below is about the SEAM: the minter here is a stub, so what is shown
            // is that a file saying nothing about rarity reaches the draw at all. That the draw lands
            // inside the record's band, and lands once, is shown on the bag with a real minter
            // (AugmentItemTests.ACopyWrittenBeforeRarityWasStoredIsDrawnIntoItsRecordsBandOnce).
            var keyed = new JObject
            {
                ["currentStance"] = Stance.Dexterity.ToString(),
                ["stances"] = new JObject(),
                ["sockets"] = new JObject
                {
                    [SocketTwo] = new JObject
                    {
                        ["augment"] = Augment,
                        ["values"] = new JObject { [Duration] = 4f },
                        ["ability"] = DexAbility,
                        ["tier"] = 2
                    }
                }
            };

            var board = new AbilitySocketBoard();
            IPassiveTreeService tree = NewTree();
            CreateService(tree, board);
            tree.Take(SocketTwo);
            new AbilityBookSaveParticipant(AccessorFor(NewBook()), board, copies: LegacyDraw(Rarity.Rare))
                .Restore(keyed, savedVersion: 5);

            Assert.AreEqual(Augment, board.Find(board.At(SocketTwo))?.Augment?.AugmentId, "a readable file lost the augment in its slot");
            Assert.AreEqual(4f, board.Find(board.At(SocketTwo))?.Augment?.Values[Duration], "the copy came back at another number");
            Assert.AreEqual(Rarity.Rare, board.Find(board.At(SocketTwo))?.Augment?.Rarity,
                "a file that says nothing about rarity was seated at whatever the enum's default is instead of a drawn one");
        }

        /// <summary>A file holding an allocation and the augments installed into the slots it opened.</summary>
        private static SaveFile Save(string[] taken, params (string Socket, AugmentInstance Augment)[] installed)
        {
            var board = new AbilitySocketBoard();
            IPassiveTreeService tree = NewTree();
            CreateService(tree, board);
            foreach (string nodeId in taken) tree.Take(nodeId);
            foreach ((string socket, AugmentInstance augment) in installed)
                Assert.IsTrue(board.Install(board.At(socket), augment), $"the fixture could not fill {socket}");

            return ManagerFor(tree, board).Capture(new SaveMetadata());
        }

        /// <summary>The same file with nothing taken in the tree — the shape of a build in which no node
        /// opens the slot the book remembers.</summary>
        private static SaveFile StrippedAllocation(SaveFile file)
        {
            var board = new AbilitySocketBoard();
            IPassiveTreeService bare = NewTree();
            CreateService(bare, board);
            SaveFile empty = ManagerFor(bare, board).Capture(new SaveMetadata());
            file.Sections[TreeSection] = empty.Sections[TreeSection];
            return file;
        }

        /// <summary>The socket entries a captured section wrote.</summary>
        private static IReadOnlyList<JToken> WrittenSockets(JToken written) => [.. written["sockets"] ?? new JArray()];

        /// <summary>A slot as the board addresses it.</summary>
        private static string Address(string socketId, string abilityId, int tier) =>
            new AbilitySocketPlacement(socketId, abilityId, tier).Address;

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
                ability.SetupGet(a => a.InstalledUpgrades).Returns(new Dictionary<string, IAugment>());
                ability.Setup(a => a.IsSame(It.IsAny<string>())).Returns((string other) => other == instanceId);
                return ability.Object;
            }

            /// <summary>These walks are about the slots and the file, never about behaviour: nothing
            /// here asks what an augment does.</summary>
            public IAugment? CreateUpgrade(AugmentInstance augment) => null;

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

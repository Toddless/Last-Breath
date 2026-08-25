namespace LastBreathTest.BattleSystemTests
{
    using Battle.Source;
    using Battle.Source.Abilities;
    using Core.Battle.Abilities;
    using Core.Data;
    using Core.Data.AbilityData;
    using Core.Data.GameData;
    using Core.Data.SaveData;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Inventory;
    using Core.Items;
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
    /// An ornament is the fourth socket: a thing the player owns, worn by one ability at a time, that
    /// grants one more slot of its own tier beside the ones the tree opens. Three rules carry the whole
    /// of it, and every walk here is about one of them.
    ///
    /// ONE TO AN ABILITY. Otherwise the three of them pile onto one button, and an ability with six
    /// sockets is an ability the player never stops pressing. The refusal stands on the board rather than
    /// in the gate above it, so a load obeys it too.
    ///
    /// REMOVE-ONLY. An ornament comes off only once its socket is empty — the same rule a slot whose node
    /// was given back lives by, and for a sharper reason: an augment carried along would land on an
    /// ability whose tags the fitting rule never measured it against. It is also what makes attaching and
    /// detaching unable to change what any ability WEARS, which is why neither road binds anything.
    ///
    /// THE TREE DOES NOT OWN IT. The slots the allocation opens are re-read on every pass and a respec
    /// takes them all; the ornament is the player's decision about a thing he owns, so the board holds it
    /// beside the augments and writes it down.
    /// </summary>
    [TestClass]
    public class OrnamentTests
    {
        private const string Seed = "start_1";
        private const string UnlockNode = "abilityunlock_dex";
        private const string SocketTwoNode = "sockettier2_dex";
        private const string SocketThreeNode = "sockettier3_dex";
        private const string OtherUnlockNode = "abilityunlock_dex_two";

        private const string Ability = "Ability_Dex";
        private const string OtherAbility = "Ability_Dex_Two";

        private const string TierOne = "Ornament_Tier_1";
        private const string TierTwo = "Ornament_Tier_2";
        private const string TierThree = "Ornament_Tier_3";

        private const string Augment = "Augment_Sharpened";
        private const string OtherAugment = "Augment_Whetted";

        private const string BookSection = "abilityBook";
        private const int BagSlots = 8;

        [TestMethod]
        public void AnOrnamentGrantsOneMoreSocketOfItsOwnTier()
        {
            // The whole of what an ornament does, and the tier is exactly its own: a tier-2 ornament on
            // an ability with a tier-2 slot leaves that ability with two of them, not with a tier-3.
            var board = new AbilitySocketBoard();
            IPassiveTreeService tree = NewTree();
            CreateUnlockService(tree, board);
            tree.Take(SocketTwoNode);

            Assert.IsTrue(board.Attach(new AbilityOrnament(TierTwo, Ability, Tier: 2)));

            IReadOnlyList<AbilitySocket> sockets = board.SocketsOf(Ability);
            Assert.AreEqual(2, sockets.Count, "the ornament opened no slot, or opened more than one");
            CollectionAssert.AreEqual(new[] { 2, 2 }, sockets.Select(socket => socket.Tier).ToArray(),
                "the granted slot takes a tier the ornament never declared");
            Assert.IsTrue(board.Find(new AbilityOrnament(TierTwo, Ability, 2).Slot.Address)?.IsOpen == true,
                "the granted slot came up remove-only");
        }

        [TestMethod]
        public void TheGrantedSlotIsAddressedLikeAnyOther()
        {
            // The address is the whole signature and the ornament is what opened the slot, so it stands
            // where a node id stands. Nothing downstream needs a case for it: an install, a save entry
            // and a console line all carry the same shape of string.
            var board = new AbilitySocketBoard();
            board.Attach(new AbilityOrnament(TierThree, Ability, Tier: 3));

            AbilitySocket granted = board.SocketsOf(Ability).Single();
            Assert.AreEqual($"{TierThree}|{Ability}|3", granted.Address);
            Assert.AreEqual(TierThree, granted.SocketId, "the slot does not name what opened it");
        }

        [TestMethod]
        public void AnAbilityWearsOneOrnamentAtATime()
        {
            var board = new AbilitySocketBoard();
            Assert.IsTrue(board.Attach(new AbilityOrnament(TierTwo, Ability, Tier: 2)));

            Assert.IsFalse(board.Attach(new AbilityOrnament(TierThree, Ability, Tier: 3)),
                "a second ornament went onto the same ability");
            Assert.AreEqual(1, board.Ornaments.Count);
            Assert.AreEqual(TierTwo, board.OrnamentOn(Ability)?.OrnamentId, "the refusal replaced the first ornament");
            Assert.AreEqual(1, board.SocketsOf(Ability).Count, "the refused ornament left a slot behind anyway");
        }

        [TestMethod]
        public void OneOrnamentCannotBeWornInTwoPlaces()
        {
            // There are three of them in the whole game. Moving one is taking it off and putting it on,
            // so that at no point does a single artefact grant two sockets.
            var board = new AbilitySocketBoard();
            board.Attach(new AbilityOrnament(TierThree, Ability, Tier: 3));

            Assert.IsFalse(board.Attach(new AbilityOrnament(TierThree, OtherAbility, Tier: 3)));
            Assert.AreEqual(0, board.SocketsOf(OtherAbility).Count);
            Assert.AreEqual(Ability, board.Attachment(TierThree)?.AbilityId);
        }

        [TestMethod]
        public void FourAugmentsIsTheMostAnAbilityCanEverWear()
        {
            // The consequence of the two rules above, stated as a number: three slots from the tree —
            // one of each tier — plus one ornament. Two of a tier and one each of the others; a fifth
            // would need a second ornament, and there is no road to one.
            var board = new AbilitySocketBoard();
            IPassiveTreeService tree = NewTree();
            CreateUnlockService(tree, board);
            tree.Take(UnlockNode);
            tree.Take(SocketTwoNode);
            tree.Take(SocketThreeNode);
            Assert.AreEqual(3, board.SocketsOf(Ability).Count, "the tree opened something other than one slot per tier");

            board.Attach(new AbilityOrnament(TierThree, Ability, Tier: 3));

            IReadOnlyList<AbilitySocket> sockets = board.SocketsOf(Ability);
            Assert.AreEqual(4, sockets.Count);
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 3 }, sockets.Select(socket => socket.Tier).ToArray(),
                "the fourth socket is not a second one of the ornament's tier");
            foreach (AbilitySocket socket in sockets)
                Assert.IsTrue(board.Install(socket.Address, Copy(Augment)), $"{socket.Address} would not take an augment");

            Assert.AreEqual(4, board.Occupants.Count, "an ability wore more or fewer than four augments");
            Assert.IsFalse(board.Attach(new AbilityOrnament(TierTwo, Ability, Tier: 2)), "a fifth socket was reachable");
        }

        [TestMethod]
        public void ARespecDoesNotTouchTheOrnament()
        {
            // The tree's slots go with the points that paid for them. The ornament was never paid for
            // with a point, so it and what stands in it keep working.
            var board = new AbilitySocketBoard();
            IPassiveTreeService tree = NewTree();
            CreateUnlockService(tree, board);
            tree.Take(UnlockNode);
            tree.Take(SocketTwoNode);
            board.Attach(new AbilityOrnament(TierTwo, Ability, Tier: 2));
            string granted = new AbilityOrnament(TierTwo, Ability, 2).Slot.Address;
            Assert.IsTrue(board.Install(granted, Copy(Augment)));

            tree.Respec();

            Assert.AreEqual(1, board.Ornaments.Count, "the respec took the ornament");
            Assert.IsTrue(board.Find(granted)?.IsOpen == true, "the respec closed the slot the ornament grants");
            Assert.AreEqual(Augment, board.Find(granted)?.WorkingAugment?.AugmentId,
                "the augment in the ornament stopped working because the tree changed");
        }

        [TestMethod]
        public void AnAllocationThatOpensNothingStillLeavesTheOrnamentStanding()
        {
            // The sharper form of the rule: Sync is total and authoritative over the tree's slots, and
            // the ornament's is simply not the tree's to name.
            var board = new AbilitySocketBoard();
            board.Attach(new AbilityOrnament(TierOne, Ability, Tier: 1));

            board.Sync([]);

            Assert.AreEqual(1, board.SocketsOf(Ability).Count, "an empty allocation retired the ornament's slot");
            Assert.IsTrue(board.Sockets.Single().IsOpen);
        }

        [TestMethod]
        public void AnOrnamentComesOffOnlyOnceItsSocketIsEmpty()
        {
            var board = new AbilitySocketBoard();
            board.Attach(new AbilityOrnament(TierThree, Ability, Tier: 3));
            string granted = new AbilityOrnament(TierThree, Ability, 3).Slot.Address;
            board.Install(granted, Copy(Augment));

            Assert.IsFalse(board.Detach(TierThree), "the ornament came off carrying an augment");
            Assert.AreEqual(Augment, board.Find(granted)?.Augment?.AugmentId, "the refused detach moved the augment");

            Assert.AreEqual(Augment, board.Extract(granted)?.AugmentId);
            Assert.IsTrue(board.Detach(TierThree));
            Assert.AreEqual(0, board.Ornaments.Count);
            Assert.IsNull(board.Find(granted), "the slot outlived the ornament that opened it");
        }

        [TestMethod]
        public void ANewPlaythroughWearsNoOrnament()
        {
            // The board is a singleton and outlives the character, so the ornaments have to be cleared
            // by the reset itself — nothing else knows about them.
            var board = new AbilitySocketBoard();
            board.Attach(new AbilityOrnament(TierThree, Ability, Tier: 3));

            NewSessionOver(board).ResetSession();

            Assert.AreEqual(0, board.Ornaments.Count, "the fresh playthrough wears the previous one's ornament");
            Assert.AreEqual(0, board.Sockets.Count);
        }

        [TestMethod]
        public void TheGateTakesTheOrnamentOutOfTheBagAndPutsItBack()
        {
            var bag = new SlottedBag(BagSlots);
            var board = new AbilitySocketBoard();
            IOrnamentAttachGate gate = GateOver(board, bag, BookHolding(Ability));
            bag.TryAddItem(Ornament(TierThree));

            Assert.AreEqual(OrnamentAttachResult.Attached, gate.Attach(CarriedId(bag, TierThree), Ability));

            Assert.AreEqual(0, bag.GetTotalItemAmount(TierThree), "the ornament is worn and carried at once");
            Assert.AreEqual(Ability, board.Attachment(TierThree)?.AbilityId);

            Assert.AreEqual(OrnamentDetachResult.Detached, gate.Detach(TierThree));

            Assert.AreEqual(1, bag.GetTotalItemAmount(TierThree), "the ornament came off and went nowhere");
            Assert.AreEqual(0, board.Ornaments.Count);
        }

        [TestMethod]
        public void TheGateRefusesASecondOrnamentAndKeepsItInTheBag()
        {
            var bag = new SlottedBag(BagSlots);
            var board = new AbilitySocketBoard();
            IOrnamentAttachGate gate = GateOver(board, bag, BookHolding(Ability));
            bag.TryAddItem(Ornament(TierTwo));
            bag.TryAddItem(Ornament(TierThree));
            gate.Attach(CarriedId(bag, TierTwo), Ability);

            Assert.AreEqual(
                OrnamentAttachResult.AbilityAlreadyOrnamented,
                gate.Attach(CarriedId(bag, TierThree), Ability));

            Assert.AreEqual(1, bag.GetTotalItemAmount(TierThree), "the refused ornament left the bag anyway");
            Assert.AreEqual(TierTwo, board.OrnamentOn(Ability)?.OrnamentId);
        }

        [TestMethod]
        public void AnOrnamentGoesOnlyOntoAnAbilityThePlayerHas()
        {
            // The single gate the design puts on an ornament. Deliberately the only one: an open socket
            // of the same tier is NOT required, which is what lets the tier-3 ornament land on an ability
            // the build never invested a point in.
            var bag = new SlottedBag(BagSlots);
            var board = new AbilitySocketBoard();
            IOrnamentAttachGate gate = GateOver(board, bag, BookHolding(Ability));
            bag.TryAddItem(Ornament(TierThree));

            Assert.AreEqual(
                OrnamentAttachResult.AbilityNotUnlocked,
                gate.Attach(CarriedId(bag, TierThree), OtherAbility));
            Assert.AreEqual(0, board.Ornaments.Count);

            // The control: the same ornament, the same bag, an ability he does hold — and no socket of
            // tier 3 anywhere on it.
            Assert.AreEqual(OrnamentAttachResult.Attached, gate.Attach(CarriedId(bag, TierThree), Ability));
        }

        [TestMethod]
        public void TheGateRefusesToTakeOffAnOrnamentWhoseSocketIsFull()
        {
            var bag = new SlottedBag(BagSlots);
            var board = new AbilitySocketBoard();
            IOrnamentAttachGate gate = GateOver(board, bag, BookHolding(Ability));
            bag.TryAddItem(Ornament(TierThree));
            gate.Attach(CarriedId(bag, TierThree), Ability);
            board.Install(new AbilityOrnament(TierThree, Ability, 3).Slot.Address, Copy(Augment));

            Assert.AreEqual(OrnamentDetachResult.SocketNotEmpty, gate.Detach(TierThree));

            Assert.AreEqual(0, bag.GetTotalItemAmount(TierThree), "the refused detach minted a second ornament");
            Assert.AreEqual(1, board.Ornaments.Count);
        }

        [TestMethod]
        public void AFullBagRefusesTheDetachInsteadOfLosingTheOrnament()
        {
            var bag = new SlottedBag(slots: 1);
            var board = new AbilitySocketBoard();
            IOrnamentAttachGate gate = GateOver(board, bag, BookHolding(Ability));
            bag.TryAddItem(Ornament(TierThree));
            gate.Attach(CarriedId(bag, TierThree), Ability);
            bag.TryAddItem(Ornament(TierTwo));

            Assert.AreEqual(OrnamentDetachResult.NoBagRoom, gate.Detach(TierThree));

            Assert.AreEqual(1, board.Ornaments.Count, "the ornament came off into a bag that could not hold it");
        }

        [TestMethod]
        public void AnOrnamentAndTheAugmentInItSurviveASaveAndLoad()
        {
            // The whole point of the section carrying them: the ornament is the property, and the augment
            // in its socket has to land in a LIVE slot rather than in a remove-only one conjured by the
            // entry itself.
            SaveFile file = Save(
                [UnlockNode],
                [new AbilityOrnament(TierThree, Ability, Tier: 3)],
                (new AbilityOrnament(TierThree, Ability, 3).Slot.Address, Copy(Augment)));

            var board = new AbilitySocketBoard();
            IPassiveTreeService tree = NewTree();
            CreateUnlockService(tree, board);
            ManagerFor(tree, board).Restore(file);

            string granted = new AbilityOrnament(TierThree, Ability, 3).Slot.Address;
            Assert.AreEqual(TierThree, board.OrnamentOn(Ability)?.OrnamentId, "the ornament did not come back");
            Assert.AreEqual(3, board.Find(granted)?.Tier, "the granted slot came back at another tier");
            Assert.IsTrue(board.Find(granted)?.IsOpen == true, "the ornament's slot came back remove-only");
            Assert.AreEqual(Augment, board.Find(granted)?.WorkingAugment?.AugmentId,
                "the augment came back asleep in the ornament");
        }

        [TestMethod]
        public void AnOrnamentWithAnEmptySocketSurvivesASaveAndLoad()
        {
            // An empty slot is not written down — it comes back with the node. An ornament is not a node:
            // forget it and the artefact is gone from the playthrough for good.
            SaveFile file = Save([UnlockNode], [new AbilityOrnament(TierOne, Ability, Tier: 1)]);

            var board = new AbilitySocketBoard();
            IPassiveTreeService tree = NewTree();
            CreateUnlockService(tree, board);
            ManagerFor(tree, board).Restore(file);

            Assert.AreEqual(TierOne, board.OrnamentOn(Ability)?.OrnamentId, "the file spent an ornament the player owns");
        }

        [TestMethod]
        public void BothAugmentsOfADoubledTierComeBackSeated()
        {
            // Two slots of one tier on one ability, one from the tree and one from the ornament, each
            // with an augment in it. Both are live after the load — the pair is the reason the ornament
            // exists at all.
            string granted = new AbilityOrnament(TierTwo, Ability, 2).Slot.Address;
            SaveFile file = Save(
                [UnlockNode, SocketTwoNode],
                [new AbilityOrnament(TierTwo, Ability, Tier: 2)],
                ($"{SocketTwoNode}|{Ability}|2", Copy(Augment)),
                (granted, Copy(OtherAugment)));

            var board = new AbilitySocketBoard();
            IPassiveTreeService tree = NewTree();
            CreateUnlockService(tree, board);
            ManagerFor(tree, board).Restore(file);

            CollectionAssert.AreEquivalent(
                new[] { Augment, OtherAugment },
                board.SocketsOf(Ability)
                    .Where(socket => socket.Tier == 2)
                    .Select(socket => socket.WorkingAugment?.AugmentId)
                    .ToArray(),
                "one of the two tier-2 augments is not working after the load");
        }

        [TestMethod]
        public void AFileWrittenBeforeOrnamentsLoadsAsACharacterWearingNone()
        {
            // The one migration that needs no code: a file that names no ornament is a character wearing
            // none, which is exactly what every file before version 7 says.
            var data = new AbilityBookSaveData { CurrentStance = Stance.Dexterity };
            data.Sockets.Add(new SocketSaveData
            {
                Socket = SocketTwoNode, Augment = Augment, Ability = Ability, Tier = 2, Rarity = Rarity.Rare
            });

            var board = new AbilitySocketBoard();
            IPassiveTreeService tree = NewTree();
            CreateUnlockService(tree, board);
            tree.Take(SocketTwoNode);
            Participant(board).Restore(JToken.FromObject(data), savedVersion: 6);

            Assert.AreEqual(0, board.Ornaments.Count);
            Assert.AreEqual(Augment, board.Find($"{SocketTwoNode}|{Ability}|2")?.WorkingAugment?.AugmentId,
                "reading a file without ornaments cost it its augments");
        }

        [TestMethod]
        public void AnOrnamentNoRecordDeclaresIsKeptInTheFileRatherThanSpent()
        {
            // A tier cannot be invented here and cannot be read off the file, and with no record there is
            // nothing to mint either — so the bag cannot rescue this one. What must NOT happen is losing
            // it: a worn ornament is in no bag, and each comes from a quest that runs once, so the entry
            // is written out again and the build that can read the id finds it.
            var data = new AbilityBookSaveData { CurrentStance = Stance.Dexterity };
            data.Ornaments.Add(new OrnamentSaveData { Ornament = "Ornament_No_File_Declares", Ability = Ability });

            var bag = new SlottedBag(BagSlots);
            var board = new AbilitySocketBoard();
            // One participant across both halves: what it could not place it holds until it writes again,
            // which is exactly how the game holds it — the section is registered once and outlives loads.
            AbilityBookSaveParticipant participant = Participant(board, bag);
            participant.Restore(JToken.FromObject(data), savedVersion: 7);

            Assert.AreEqual(0, board.Ornaments.Count);
            Assert.AreEqual(0, board.Sockets.Count, "an unknown ornament opened a slot anyway");

            CollectionAssert.AreEqual(
                new[] { "Ornament_No_File_Declares" },
                WrittenOrnaments(participant.Capture()).ToArray(),
                "the next save spent an artefact this build merely could not read");
        }

        [TestMethod]
        public void AnOrnamentTheBoardWillNotTakeBackGoesToTheBag()
        {
            // The hole this road was rebuilt to close. A worn ornament exists nowhere but in this section,
            // so an entry the invariant refuses used to be the last trace of an artefact from an
            // unrepeatable quest. Now it comes back as a thing, exactly as taking one off an ability does.
            var data = new AbilityBookSaveData { CurrentStance = Stance.Dexterity };
            data.Ornaments.Add(new OrnamentSaveData { Ornament = TierTwo, Ability = Ability });
            data.Ornaments.Add(new OrnamentSaveData { Ornament = TierThree, Ability = Ability });

            var bag = new SlottedBag(BagSlots);
            var board = new AbilitySocketBoard();
            AbilityBookSaveParticipant participant = Participant(board, bag);
            participant.Restore(JToken.FromObject(data), savedVersion: 7);

            Assert.AreEqual(1, board.Ornaments.Count, "a character came back wearing two ornaments on one ability");
            Assert.AreEqual(TierTwo, board.OrnamentOn(Ability)?.OrnamentId);
            Assert.AreEqual(1, bag.GetTotalItemAmount(TierThree), "the refused ornament is in no bag and on no ability");
            Assert.AreEqual(3, bag.GetContents().Select(entry => entry.Item).OfType<IOrnamentItem>().Single().Tier,
                "what came back to the bag is not the ornament the file named");
            Assert.AreEqual(0, WrittenOrnaments(participant.Capture()).Count(id => id == TierThree),
                "the ornament is in the bag AND still written as worn, which is two of it");
        }

        [TestMethod]
        public void AFileNamingOneOrnamentTwiceDoesNotMintASecondOfIt()
        {
            // The case that must NOT be handed back. The ornament is on an ability already — the first
            // entry put it there — so nothing was displaced, and returning it would make two of an artefact
            // there is one of in the game. The saving of the player's property is not a licence to duplicate.
            var data = new AbilityBookSaveData { CurrentStance = Stance.Dexterity };
            data.Ornaments.Add(new OrnamentSaveData { Ornament = TierThree, Ability = Ability });
            data.Ornaments.Add(new OrnamentSaveData { Ornament = TierThree, Ability = OtherAbility });

            var bag = new SlottedBag(BagSlots);
            var board = new AbilitySocketBoard();
            AbilityBookSaveParticipant participant = Participant(board, bag);
            participant.Restore(JToken.FromObject(data), savedVersion: 7);

            Assert.AreEqual(1, board.Ornaments.Count);
            Assert.AreEqual(Ability, board.Attachment(TierThree)?.AbilityId);
            Assert.AreEqual(0, bag.GetTotalItemAmount(TierThree), "a second copy of a one-of-a-kind artefact was minted");
            CollectionAssert.AreEqual(
                new[] { TierThree },
                WrittenOrnaments(participant.Capture()).ToArray(),
                "the phantom entry was carried forward and will be read again on every load");
        }

        [TestMethod]
        public void ADisplacedOrnamentWithNoRoomForItStaysInTheFile()
        {
            // The honest answer when the bag cannot take it: not a loss and not a bag that overflows, but
            // the entry written out again. The ornament is invisible until the player makes room, and it
            // is still his.
            var data = new AbilityBookSaveData { CurrentStance = Stance.Dexterity };
            data.Ornaments.Add(new OrnamentSaveData { Ornament = TierTwo, Ability = Ability });
            data.Ornaments.Add(new OrnamentSaveData { Ornament = TierThree, Ability = Ability });

            var bag = new SlottedBag(slots: 1);
            bag.TryAddItem(Ornament(TierOne));
            var board = new AbilitySocketBoard();
            AbilityBookSaveParticipant participant = Participant(board, bag);
            participant.Restore(JToken.FromObject(data), savedVersion: 7);

            Assert.AreEqual(0, bag.GetTotalItemAmount(TierThree), "the full bag took it anyway");
            CollectionAssert.AreEquivalent(
                new[] { TierTwo, TierThree },
                WrittenOrnaments(participant.Capture()).ToArray(),
                "the displaced ornament was spent because the bag happened to be full");
        }

        [TestMethod]
        public void AnUnplaceableOrnamentIsNotInheritedByTheNextPlaythrough()
        {
            // The participant outlives the character, so what it holds for a file has to go with that
            // file — otherwise the first save of the next playthrough carries an artefact belonging to a
            // character that no longer exists.
            var data = new AbilityBookSaveData { CurrentStance = Stance.Dexterity };
            data.Ornaments.Add(new OrnamentSaveData { Ornament = "Ornament_No_File_Declares", Ability = Ability });

            var board = new AbilitySocketBoard();
            AbilityBookSaveParticipant participant = Participant(board, new SlottedBag(BagSlots));
            participant.Restore(JToken.FromObject(data), savedVersion: 7);
            Assert.AreEqual(1, WrittenOrnaments(participant.Capture()).Count(), "nothing is being held, so nothing can leak");

            participant.RestoreWithoutSection();

            Assert.AreEqual(0, WrittenOrnaments(participant.Capture()).Count(),
                "the previous playthrough's unreadable ornament is in this one's save");
        }

        [TestMethod]
        public void AFileWithoutTheSectionTakesTheOrnamentsOffToo()
        {
            // The board outlives the character: starting a game whose file carries no ability book must
            // not leave the previous playthrough's ornaments granting sockets.
            var board = new AbilitySocketBoard();
            board.Attach(new AbilityOrnament(TierThree, Ability, Tier: 3));

            Participant(board).RestoreWithoutSection();

            Assert.AreEqual(0, board.Ornaments.Count);
            Assert.AreEqual(0, board.Sockets.Count);
        }

        [TestMethod]
        public void TheSectionDeclaresTheCurrentVersion()
        {
            // 7 brought the ornaments, 8 writes each augment's rarity by name. A bump nobody meant is a
            // file the shipped build refuses, so the number is pinned rather than derived.
            Assert.AreEqual(8, Participant(new AbilitySocketBoard()).Version);
        }

        [TestMethod]
        public void TheShippedDataDeclaresOneOrnamentPerTier()
        {
            // Three artefacts, one for each tier of socket the game has. A second ornament of one tier
            // would be a second fourth socket, which the one-to-an-ability rule was written against.
            IOrnamentCatalog catalog = ShippedOrnaments();

            CollectionAssert.AreEqual(
                new[] { 1, 2, 3 },
                catalog.All.Select(ornament => ornament.Tier).OrderBy(tier => tier).ToArray(),
                "the ornaments do not cover the tiers one to one");
            foreach (OrnamentData ornament in catalog.All)
                Assert.AreSame(ornament, catalog.Find(ornament.Id), $"'{ornament.Id}' is not found by its own id");
        }

        [TestMethod]
        public void NoPassiveNodeIsNamedAfterAnOrnament()
        {
            // The granted slot is addressed by the ornament's own id, and the board keys its slots by
            // that address. A tree node sharing the id would make one slot out of two the moment both
            // pointed at the same ability and tier — the address is honest exactly while the ids are.
            HashSet<string> nodes =
            [
                .. ((JArray?)ShippedTree()["nodes"] ?? [])
                    .Select(node => node.Value<string>("id") ?? string.Empty)
            ];

            Assert.IsTrue(nodes.Count > 0, "the shipped tree declares no node, so the walk proves nothing");
            foreach (OrnamentData ornament in ShippedOrnaments().All)
                Assert.IsFalse(nodes.Contains(ornament.Id),
                    $"a passive node is named '{ornament.Id}': its slot and the ornament's would share an address");
        }

        [TestMethod]
        public void EveryOrnamentIsHandedOutByExactlyOneQuest()
        {
            // Quests are the only road: ornaments drop from nobody. A tier handed out twice, or not at
            // all, is a playthrough that cannot reach the fourth socket it was promised.
            JArray quests = (JArray?)ShippedQuests()["quests"] ?? [];
            IOrnamentCatalog catalog = ShippedOrnaments();

            foreach (OrnamentData ornament in catalog.All)
            {
                List<string> givers =
                [
                    .. quests
                        .Where(quest => RewardIds(quest).Contains(ornament.Id, StringComparer.Ordinal))
                        .Select(quest => quest.Value<string>("id") ?? string.Empty)
                ];

                Assert.AreEqual(1, givers.Count,
                    $"'{ornament.Id}' is a reward of {givers.Count} quest(s) ({string.Join(", ", givers)}); exactly one hands it out");
            }
        }

        [TestMethod]
        public void AnOrnamentIsBornThroughTheOrdinaryItemDoor()
        {
            // The road a quest reward takes: it holds an id and calls the type-agnostic minter. An
            // ornament id falling through to a plain template copy would hand the player an item that
            // grants nothing.
            IOrnamentMinter minter = new OrnamentMinter(ShippedOrnaments());
            var items = new Mock<IItemDataProvider>();
            var door = new ItemMinter(items.Object, Mock.Of<IEquipItemMinter>(), items.Object, ornaments: minter);

            IItem minted = door.MintItem(TierThree);

            Assert.IsInstanceOfType<IOrnamentItem>(minted, "an ornament id came out as something else");
            Assert.AreEqual(3, ((IOrnamentItem)minted).Tier);
            Assert.AreEqual(1, minted.MaxStackSize, "ornaments stack, so a pile could not say which one left it");
            items.Verify(data => data.CopyItem(It.IsAny<string>()), Times.Never,
                "the id fell through to the plain template copy");
        }

        /// <summary>A file holding an allocation, the ornaments worn and the augments in the slots.</summary>
        private static SaveFile Save(
            string[] taken,
            IReadOnlyCollection<AbilityOrnament> ornaments,
            params (string Address, AugmentInstance Augment)[] installed)
        {
            var board = new AbilitySocketBoard();
            IPassiveTreeService tree = NewTree();
            CreateUnlockService(tree, board);
            foreach (string nodeId in taken) tree.Take(nodeId);
            foreach (AbilityOrnament ornament in ornaments)
                Assert.IsTrue(board.Attach(ornament), $"the fixture could not attach {ornament.OrnamentId}");
            foreach ((string address, AugmentInstance augment) in installed)
                Assert.IsTrue(board.Install(address, augment), $"the fixture could not fill {address}");

            return ManagerFor(tree, board).Capture(new SaveMetadata());
        }

        /// <summary>The ability-book section over one board, reading the shipped ornament records — what
        /// a saved ornament GRANTS comes from the catalog and from nowhere else. With a bag it can also
        /// hand back an ornament it cannot place, which is the whole of the salvage road.</summary>
        private static AbilityBookSaveParticipant Participant(IAbilitySocketBoard board, IInventory? bag = null) =>
            new(
                AccessorFor(NewBook()),
                board,
                ornaments: ShippedOrnaments(),
                ornamentMinter: new OrnamentMinter(ShippedOrnaments()),
                inventory: bag);

        /// <summary>The ornament ids a captured section wrote — the worn ones and the ones the build could
        /// not place, which is the only place the latter survive.</summary>
        private static IEnumerable<string> WrittenOrnaments(JToken written) =>
            ((JArray?)written["ornaments"] ?? []).Select(entry => entry.Value<string>("ornament") ?? string.Empty);

        private static ISaveManager ManagerFor(IPassiveTreeService tree, IAbilitySocketBoard board)
        {
            var manager = new SaveManager(new LoadScope());
            manager.Register(new PassiveTreeSaveParticipant(tree));
            manager.Register(Participant(board));
            return manager;
        }

        /// <summary>The gate as the game composes it, over a bag and a book a walk can dress.</summary>
        private static IOrnamentAttachGate GateOver(IAbilitySocketBoard board, IInventory bag, IAbilityBookComponent book) =>
            new OrnamentAttachGate(board, AccessorFor(book), new OrnamentMinter(ShippedOrnaments()), bag);

        /// <summary>A book holding those abilities and nothing else — the live truth of what the player
        /// has, which is the one gate an ornament passes.</summary>
        private static IAbilityBookComponent BookHolding(params string[] abilityIds)
        {
            AbilityBookComponent book = NewBook();
            foreach (string abilityId in abilityIds) book.Learn(Stance.Dexterity, NewAbility(abilityId));

            return book;
        }

        private static IAbility NewAbility(string abilityId)
        {
            string instanceId = Guid.NewGuid().ToString();
            var ability = new Mock<IAbility>();
            ability.SetupGet(mock => mock.Id).Returns(abilityId);
            ability.SetupGet(mock => mock.InstanceId).Returns(instanceId);
            ability.SetupGet(mock => mock.InstalledUpgrades).Returns(new Dictionary<string, IAugment>());
            ability.Setup(mock => mock.IsSame(It.IsAny<string>())).Returns((string other) => other == instanceId);
            return ability.Object;
        }

        /// <summary>One ornament as the bag holds it, built the way the game builds one.</summary>
        private static IOrnamentItem Ornament(string ornamentId) =>
            new OrnamentMinter(ShippedOrnaments()).Mint(ornamentId)
            ?? throw new InvalidOperationException($"the shipped data declares no '{ornamentId}'");

        /// <summary>The instance id the bag knows that ornament by — what the gate takes.</summary>
        private static string CarriedId(IInventory bag, string ornamentId) =>
            bag.GetContents()
                .Select(entry => entry.Item)
                .OfType<IOrnamentItem>()
                .First(item => string.Equals(item.Id, ornamentId, StringComparison.Ordinal))
                .InstanceId;

        /// <summary>The ornament records the shipped files declare, through the real loader.</summary>
        private static OrnamentCatalog ShippedOrnaments()
        {
            var catalog = new OrnamentCatalog();
            var service = new GameDataService(new FileSystemDataSource(SharedData.Root()), [catalog]);
            List<string> failures = [];
            service.LoadFailed += (context, exception) => failures.Add($"{context}: {exception.Message}");

            service.LoadAll();

            Assert.AreEqual(0, failures.Count, string.Join("; ", failures));
            return catalog;
        }

        /// <summary>The shipped quest file as written, read as markup rather than through the provider:
        /// what is being asked is which quest NAMES the reward, which is a question about the data.</summary>
        private static JObject ShippedQuests() =>
            JObject.Parse(File.ReadAllText(Path.Combine(SharedData.Catalog(DataCatalog.Quests), "Quests.json")));

        /// <summary>The shipped tree document as written — the walk is about the node IDS, which is a
        /// question about the markup and not about an allocation.</summary>
        private static JObject ShippedTree() =>
            JObject.Parse(File.ReadAllText(Path.Combine(SharedData.Catalog(DataCatalog.PassiveTree), "PassiveTree.json")));

        private static IEnumerable<string> RewardIds(JToken quest) =>
            ((JArray?)quest["rewards"]?["items"] ?? []).Select(item => item.Value<string>("itemId") ?? string.Empty);

        /// <summary>The reset stack the game builds, holding nothing but the board.</summary>
        private static ISessionResetService NewSessionOver(IAbilitySocketBoard board)
        {
            var services = new ServiceCollection();
            services.AddSingleton<LoadScope>();
            services.AddSingleton(board);
            services.AddSessionReset();
            return services.BuildServiceProvider().GetRequiredService<ISessionResetService>();
        }

        /// <summary>Two unlock nodes and two socket nodes on the first ability — one slot of each tier,
        /// which is the shape the tree hands out and the shape the fourth socket is added to.</summary>
        private static PassiveTreeService NewTree()
        {
            var document = new PassiveTreeDocument();
            document.AddNode(new PassiveNode { Id = Seed, Kind = PassiveNodeKind.Start, Stance = Stance.Strength });

            Add(UnlockNode, PassiveNodeKind.AbilityUnlock, Ability);
            Add(OtherUnlockNode, PassiveNodeKind.AbilityUnlock, OtherAbility);
            Add(SocketTwoNode, PassiveNodeKind.SocketTier2, Ability);
            Add(SocketThreeNode, PassiveNodeKind.SocketTier3, Ability);

            var service = new PassiveTreeService(new OrnamentTreeStub(document), ConditionCatalogs.Empty());
            service.SetTotalPoints(document.Nodes.Count);
            return service;

            void Add(string id, PassiveNodeKind kind, string abilityId)
            {
                document.AddNode(new PassiveNode { Id = id, Kind = kind, AbilityId = abilityId });
                document.Link(Seed, id);
            }
        }

        private static AbilityUnlockService CreateUnlockService(IPassiveTreeService tree, IAbilitySocketBoard board) =>
            new(AccessorFor(NewBook()), new OrnamentAbilityProvider(), tree, board);

        /// <summary>Catalog stub: two dexterity abilities, nothing hidden.</summary>
        private sealed class OrnamentAbilityProvider : IAbilityProvider
        {
            public IReadOnlyCollection<string> KnownAbilityIds => [Ability, OtherAbility];

            public IAbility CreateAbility(string abilityId) => NewAbility(abilityId);

            /// <summary>These walks are about the slots and the file, never about behaviour.</summary>
            public IAugment? CreateUpgrade(AugmentInstance augment) => null;

            public Stance GetAbilityStance(string abilityId) => Stance.Dexterity;

            public bool IsHidden(string abilityId) => false;
        }

        private sealed class OrnamentTreeStub(PassiveTreeDocument tree) : IPassiveTreeProvider
        {
            public PassiveTreeDocument Tree { get; } = tree;

            public IReadOnlyList<string> Issues => [];
        }
    }
}

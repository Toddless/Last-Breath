namespace LastBreathTest.BattleSystemTests
{
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Items;
    using Core.Narrative.Facts;
    using Core.Save;
    using Core.Services;
    using Moq;

    /// <summary>
    /// What the world remembers about the player's gear. The fact is "has ever worn", so the whole of it
    /// is: a piece put on raises its own kind and counts toward wearing anything at all, the same is not
    /// written for anybody else's gear, taking the piece off leaves the memory standing, and a paperdoll
    /// dressed by a save file is not lived through a second time.
    /// </summary>
    [TestClass]
    public class EquipFactTrackerTests
    {
        private const string HelmetId = "Helmet_Iron";

        private const string RingId = "Ring_Plain";

        private WorldFactsService _facts = null!;

        private LoadScope _load = null!;

        private PlayerAccessor _players = null!;

        [TestInitialize]
        public void Setup()
        {
            _facts = new WorldFactsService();
            _load = new LoadScope();
            _players = new PlayerAccessor();
        }

        [TestMethod]
        public void APiecePutOn_RaisesItsOwnKindAndCountsTowardWearingAnythingAtAll()
        {
            IEquipmentComponent paperdoll = Paperdoll();
            Track();

            paperdoll.TryEquip(Gear(EquipmentPiece.Helmet, HelmetId), out _);

            Assert.AreEqual(1, _facts.GetCount(FactKeys.ItemEquipped(EquipmentPiece.Helmet)),
                "the kind of gear the player put on went unremembered");
            Assert.AreEqual(1, _facts.GetCount(FactKeys.ItemEquippedAny()),
                "wearing gear at all went uncounted");
        }

        /// <summary>The player is a scene node registering himself, so the tracker is as often built
        /// before his arrival as after it.</summary>
        [TestMethod]
        public void APlayerArrivingAfterTheTracker_IsFollowedAllTheSame()
        {
            Track();
            IEquipmentComponent paperdoll = Paperdoll();

            paperdoll.TryEquip(Gear(EquipmentPiece.Helmet, HelmetId), out _);

            Assert.AreEqual(1, _facts.GetCount(FactKeys.ItemEquipped(EquipmentPiece.Helmet)),
                "a player who arrived after the tracker was never listened to");
        }

        /// <summary>A replaced player takes the ear with him: the old paperdoll is heard no more, and the
        /// new one is heard at once. A tracker still listening to the doll left behind would write the
        /// world's facts off a body nobody is playing.</summary>
        [TestMethod]
        public void APlayerReplaced_TakesTheEarWithHim()
        {
            IEquipmentComponent left = Paperdoll();
            Track();
            IEquipmentComponent arrived = Paperdoll();

            left.TryEquip(Gear(EquipmentPiece.Helmet, HelmetId), out _);

            Assert.AreEqual(0, _facts.Snapshot.Count, "the paperdoll of a replaced player was still listened to");

            arrived.TryEquip(Gear(EquipmentPiece.Ring, RingId), out _);

            Assert.AreEqual(1, _facts.GetCount(FactKeys.ItemEquipped(EquipmentPiece.Ring)),
                "the player who arrived in the old one's place was never listened to");
        }

        [TestMethod]
        public void TheGearOfSomebodyElse_IsNotRememberedAsThePlayers()
        {
            Paperdoll();
            Track();
            var stranger = new EquipmentComponent(Mock.Of<IFightable>());

            stranger.TryEquip(Gear(EquipmentPiece.Helmet, HelmetId), out _);

            Assert.AreEqual(0, _facts.Snapshot.Count, "somebody else's gear was written down as the player's");
        }

        /// <summary>"Has ever worn" and not "is wearing": the flag is what a quest asking for a first
        /// piece of gear reads, and undressing does not undo a thing already done.</summary>
        [TestMethod]
        public void TakingThePieceOff_LeavesTheMemoryStanding()
        {
            IEquipmentComponent paperdoll = Paperdoll();
            Track();
            paperdoll.TryEquip(Gear(EquipmentPiece.Helmet, HelmetId), out _);

            paperdoll.TryUnequip(EquipmentPiece.Helmet, out _);

            Assert.AreEqual(1, _facts.GetCount(FactKeys.ItemEquipped(EquipmentPiece.Helmet)),
                "taking the helmet off unlearnt ever having worn one");
            Assert.AreEqual(1, _facts.GetCount(FactKeys.ItemEquippedAny()),
                "the count of gear ever worn was taken back");
        }

        /// <summary>A save restores the paperdoll by equipping everything the file held, and the facts of
        /// that run are restored beside it: reliving the dressing would count every load.</summary>
        [TestMethod]
        public void APaperdollDressedByASaveFile_IsNotLivedThroughAgain()
        {
            IEquipmentComponent paperdoll = Paperdoll();
            Track();

            using (_load.Begin())
                paperdoll.TryEquip(Gear(EquipmentPiece.Helmet, HelmetId), out _);

            Assert.AreEqual(0, _facts.Snapshot.Count, "a load wrote the facts the file already carries");
        }

        /// <summary>The second ring lands in the second slot of the paperdoll, and a ring is a ring: the
        /// fact is about the kind of gear, not about which finger took it.</summary>
        [TestMethod]
        public void ARingOnEitherFinger_AnswersUnderTheOneKind()
        {
            IEquipmentComponent paperdoll = Paperdoll();
            Track();

            paperdoll.TryEquip(Gear(EquipmentPiece.Ring, RingId), out _);
            paperdoll.TryEquip(Gear(EquipmentPiece.Ring, RingId), out _);

            Assert.AreEqual(0, _facts.GetCount(FactKeys.ItemEquipped(EquipmentPiece.Ring2)),
                "a ring slot of the paperdoll was written as a kind of gear");
            Assert.AreEqual(1, _facts.GetCount(FactKeys.ItemEquipped(EquipmentPiece.Ring)),
                "the second ring was remembered as something other than a ring");
            Assert.AreEqual(2, _facts.GetCount(FactKeys.ItemEquippedAny()),
                "both rings were meant to count as gear put on");
        }

        private EquipFactTracker Track() => new(_players, _facts, _load);

        /// <summary>A player with a paperdoll, announced the way the scene announces him.</summary>
        private EquipmentComponent Paperdoll()
        {
            var player = new Mock<IPlayer>();
            var equipment = new EquipmentComponent(player.Object);
            player.SetupGet(fighter => fighter.Equipment).Returns(equipment);
            _players.Set(player.Object);
            return equipment;
        }

        private static EquipItem Gear(EquipmentPiece piece, string id) => new(piece, id, []);
    }
}

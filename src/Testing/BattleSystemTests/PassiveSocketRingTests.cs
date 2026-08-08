namespace LastBreathTest.BattleSystemTests
{
    using Core.Battle.Abilities;
    using Core.Enums;
    using Core.PassiveTree;
    using Core.PassiveTree.View;
    using Core.Views.UI;

    /// <summary>
    /// The ring of augment slots the wheel draws around an ability node: what it is made of, what each
    /// place of it is doing, and where a place sits.
    /// <para>Two readings that must not mix. Composition comes from the DOCUMENT and changes once a
    /// catalog load; state comes from the BOARD and changes on every purchase. What binds them is the
    /// ORDER — the board's own, tier then address — because the Nth pip a player clicks has to be the Nth
    /// slot the board hands over.</para>
    /// </summary>
    [TestClass]
    public class PassiveSocketRingTests
    {
        private const string Ability = "Ability_Dex";
        private const string OtherAbility = "Ability_Str";

        private const string Seed = "seed";
        private const string CoreSeed = "seed_core";
        private const string Unlock = "unlock";

        /// <summary>Two socket ids sharing a prefix. Ordered one way by id and the other by address —
        /// the separator sorts after letters — which is the whole reason the order is the board's.</summary>
        private const string ShortSocket = "ab";

        private const string LongSocket = "abc";

        [TestMethod]
        public void ARingIsOrderedTheWayTheBoardOrdersItsSlots()
        {
            PassiveTreeDocument document = Tree();
            Dictionary<string, List<SocketRingSlot>> rings = [];

            SocketRings.Collect(document, rings);

            Assert.IsTrue(string.CompareOrdinal(ShortSocket, LongSocket) < 0,
                "the fixture must hold two ids that order differently by id and by address");

            List<SocketRingSlot> ring = rings[Unlock];
            Assert.AreEqual(NodeKindRules.UnlockSocketTier, ring[0].Tier, "the ability's own slot is not first");
            Assert.AreEqual(Unlock, ring[0].OpenerId, "the tier-one slot is opened by something other than the ability node");

            CollectionAssert.AreEqual(
                BoardOver(document).SocketsOf(Ability).Select(socket => socket.Address).ToArray(),
                ring.Select(slot => slot.Address).ToArray(),
                "the pips and the slots would not line up");
        }

        [TestMethod]
        public void AnAbilityWithNoSocketNodesStillHasTheSlotItsUnlockBringsWithIt()
        {
            PassiveTreeDocument document = Tree();
            Dictionary<string, List<SocketRingSlot>> rings = [];

            SocketRings.Collect(document, rings);

            List<SocketRingSlot> ring = rings[Seed];
            Assert.AreEqual(1, ring.Count, "the tier-one slot that comes with the ability went missing");
            Assert.AreEqual(NodeKindRules.UnlockSocketTier, ring[0].Tier);
        }

        /// <summary>The seed at the middle of the wheel belongs to no stance and names no ability. It
        /// opens nothing, so it has nothing to draw a ring of.</summary>
        [TestMethod]
        public void ASeedThatNamesNoAbilityHasNoRing()
        {
            PassiveTreeDocument document = Tree();
            Dictionary<string, List<SocketRingSlot>> rings = [];

            SocketRings.Collect(document, rings);

            Assert.IsFalse(rings.ContainsKey(CoreSeed));
            Assert.IsFalse(rings.ContainsKey(ShortSocket), "a socket node grew a ring of its own");
        }

        /// <summary>
        /// The state of a place comes from the board and from nowhere else, and a CLOSED slot is part of
        /// the vocabulary: the node behind it was given back, the player's augment is still in it, and
        /// the ring is where he finds out.
        /// </summary>
        [TestMethod]
        public void AClosedSlotHoldingAnAugmentReadsAsHeld()
        {
            PassiveTreeDocument document = Tree();
            IAbilitySocketBoard board = BoardOver(document);
            Dictionary<string, List<SocketRingSlot>> rings = [];
            SocketRings.Collect(document, rings);

            SocketRingSlot slot = rings[Unlock].Single(entry => entry.OpenerId == LongSocket);
            Assert.AreEqual(SocketSlotState.Open, SocketRings.StateOf(board.Find(slot.Address)));

            Assert.IsTrue(board.Install(slot.Address, new AugmentInstance("Augment_Any", new Dictionary<string, float>())));
            Assert.AreEqual(SocketSlotState.Filled, SocketRings.StateOf(board.Find(slot.Address)));

            // The node behind it goes back: every other slot stays live and this one closes around what
            // the player owns.
            board.Sync([.. Placements(document).Where(placement => placement.SocketId != LongSocket)]);

            Assert.AreEqual(SocketSlotState.Held, SocketRings.StateOf(board.Find(slot.Address)),
                "the player's augment fell out of the ring together with the node that was holding it");
        }

        /// <summary>A place whose node nobody has bought has no socket on the board at all — which is the
        /// same fact as "not owned", read once.</summary>
        [TestMethod]
        public void APlaceWhoseNodeIsNotOwnedReadsAsUnopened()
        {
            PassiveTreeDocument document = Tree();
            var board = new AbilitySocketBoard();
            Dictionary<string, List<SocketRingSlot>> rings = [];
            SocketRings.Collect(document, rings);

            foreach (SocketRingSlot slot in rings[Unlock])
                Assert.AreEqual(SocketSlotState.Unopened, SocketRings.StateOf(board.Find(slot.Address)));
        }

        /// <summary>
        /// The two refusals the wheel owes before the install gate is asked. A node nobody has bought
        /// must not be answered with "there is no such socket" — that sends the player looking for a slot
        /// when what he is missing is a point — and a node the plan means to give back must not swallow an
        /// augment into a slot that is about to close.
        /// </summary>
        [TestMethod]
        public void ADropIsRefusedByTheWheelBeforeTheGateIsAsked()
        {
            Assert.AreEqual(PassiveSocketRefusalText.NotOpened,
                PassiveSocketRefusalText.KeyFor(openerTaken: false, plannedReturn: false));

            Assert.AreEqual(PassiveSocketRefusalText.PlannedRefund,
                PassiveSocketRefusalText.KeyFor(openerTaken: true, plannedReturn: true));

            Assert.IsNull(PassiveSocketRefusalText.KeyFor(openerTaken: true, plannedReturn: false),
                "the wheel answered a question that belongs to the install gate");
        }

        /// <summary>A pip is picked exactly where it is drawn: the two radii are one number read in two
        /// spaces, and the centre is the node's own place plus a ring radius measured off the node.</summary>
        [TestMethod]
        public void APipIsPickedWhereItIsDrawn()
        {
            const float ringScale = 2.1f;
            NodeGeometry nodes = Geometry();
            var geometry = new SocketRingGeometry(nodes, ringScale, 0.55f, 120f, 2f, 6f);
            var view = new CanvasTransform();
            view.SetSpread(2.25f, 0f, 0f);
            view.ZoomBy(2f, 0f, 0f);
            view.SetPan(640f, 360f);

            const float nodeX = 120f;
            const float nodeY = -80f;
            float ring = nodes.DocumentRingRadius(PassiveNodeKind.AbilityUnlock, view, ringScale);

            for (int index = 0; index < 3; index++)
            {
                SocketPipPlacement pip = geometry.Place(nodeX, nodeY, PassiveNodeKind.AbilityUnlock, view, index, 3);

                float distance = MathF.Sqrt(
                    (pip.X - nodeX) * (pip.X - nodeX) + (pip.Y - nodeY) * (pip.Y - nodeY));

                Assert.AreEqual(ring, distance, 0.01f, "the pip does not sit on the ring it is drawn on");
                Assert.AreEqual(pip.ScreenRadius, pip.DocumentRadius * view.PositionScale, 0.01f,
                    "what is drawn and what answers a click are two different sizes");
            }
        }

        /// <summary>A node too small on screen wears no ring at all — a mark on a dot a pixel and a half
        /// across has stopped being that dot's mark — and the rule is one, so the pick agrees.</summary>
        [TestMethod]
        public void ARingStopsBeingDrawnBeforeItStopsBeingAimable()
        {
            NodeGeometry nodes = Geometry();
            var geometry = new SocketRingGeometry(nodes, 2.1f, 0.55f, 120f, 2f, 6f);

            Assert.IsTrue(geometry.Draws(PassiveNodeKind.AbilityUnlock, 1f));
            Assert.IsFalse(geometry.Draws(PassiveNodeKind.AbilityUnlock, CanvasTransform.MinZoom),
                "the whole tree on screen still drew rings on dots nobody can aim at");
        }

        private static NodeGeometry Geometry()
        {
            var radii = new Dictionary<PassiveNodeKind, float>();
            float radius = 5f;
            foreach (PassiveNodeKind kind in Enum.GetValues<PassiveNodeKind>()) radii[kind] = radius++;

            return new NodeGeometry(radii, 1.5f, 6f);
        }

        /// <summary>The board the allocation would build with everything taken — the same placements the
        /// unlock service collects, so the order under test is the order the game produces.</summary>
        private static IAbilitySocketBoard BoardOver(PassiveTreeDocument document)
        {
            var board = new AbilitySocketBoard();
            board.Sync(Placements(document));
            return board;
        }

        private static List<AbilitySocketPlacement> Placements(PassiveTreeDocument document)
        {
            List<AbilitySocketPlacement> placements = [];
            foreach (PassiveNode node in document.Nodes)
            {
                int tier = NodeKindRules.SocketTier(node.Kind);
                if (tier == NodeKindRules.NoSocket || node.AbilityId.Length == 0) continue;

                placements.Add(new AbilitySocketPlacement(node.Id, node.AbilityId, tier));
            }

            return placements;
        }

        /// <summary>A neutral seed, a stance seed carrying its ability, an unlock node with two tier-two
        /// sockets whose ids share a prefix, and a tier-three socket of another ability.</summary>
        private static PassiveTreeDocument Tree()
        {
            var document = new PassiveTreeDocument();
            document.AddNode(new PassiveNode { Id = CoreSeed, Kind = PassiveNodeKind.Start });
            document.AddNode(new PassiveNode
            {
                Id = Seed,
                Kind = PassiveNodeKind.Start,
                Stance = Stance.Dexterity,
                AbilityId = OtherAbility
            });

            Add(Unlock, PassiveNodeKind.AbilityUnlock, Ability);
            Add(ShortSocket, PassiveNodeKind.SocketTier2, Ability);
            Add(LongSocket, PassiveNodeKind.SocketTier2, Ability);
            Add("socket_three", PassiveNodeKind.SocketTier3, Ability);

            return document;

            void Add(string id, PassiveNodeKind kind, string abilityId)
            {
                document.AddNode(new PassiveNode { Id = id, Kind = kind, AbilityId = abilityId });
                document.Link(CoreSeed, id);
            }
        }
    }
}

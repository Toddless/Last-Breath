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

        /// <summary>Ring and pip sizes as multiples of the node's own radius, and the width of the fan —
        /// the shape of the game's resource rather than its numbers, which live in a .tres.</summary>
        private const float RingScale = 2.1f;

        private const float PipScale = 0.55f;
        private const float ArcDegrees = 120f;

        /// <summary>The node whose ring is placed, and where it stands — away from the middle of the
        /// wheel, so a fan aimed outward and a fan laid in a gap are two different pictures.</summary>
        private const string RingNode = "ring";

        private const float RingNodeX = 120f;
        private const float RingNodeY = -80f;

        /// <summary>How far out the fixture hangs a branch. Length says nothing here — only the direction
        /// is read — so any reach clear of the node does.</summary>
        private const float BranchLength = 100f;

        /// <summary>Slack on an angle in degrees: the arithmetic is single-precision trigonometry, and
        /// nothing under test turns on a tenth of a degree.</summary>
        private const float ToleranceDegrees = 0.1f;

        /// <summary>How many places the fan under test carries — an ability's own slot and two socket
        /// nodes, which is what a full ring of the shipped tree holds.</summary>
        private const int PipsPerRing = 3;

        /// <summary>Branches leaving at a quarter, a half and none of a turn: one gap of half a turn and
        /// two of a quarter, so the fan has one place to go and room to spare on both sides of it.</summary>
        private static readonly float[] Branches = [0f, 90f, 180f];

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

            Assert.IsTrue(board.Install(slot.Address, new AugmentInstance("Augment_Any", new Dictionary<string, float>(), Rarity.Common)));
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
            NodeGeometry nodes = Geometry();
            SocketRingGeometry geometry = Rings();
            PassiveTreeDocument document = Branching("branch", Branches);
            PassiveNode node = document.Find(RingNode)!;
            CanvasTransform view = View();
            SocketRingFan fan = geometry.Fan(document, node, PipsPerRing);

            float ring = nodes.DocumentRingRadius(PassiveNodeKind.AbilityUnlock, view, RingScale);

            for (int index = 0; index < PipsPerRing; index++)
            {
                SocketPipPlacement pip = geometry.Place(fan, node, view, index, PipsPerRing);

                float distance = MathF.Sqrt(
                    (pip.X - RingNodeX) * (pip.X - RingNodeX) + (pip.Y - RingNodeY) * (pip.Y - RingNodeY));

                Assert.AreEqual(ring, distance, 0.01f, "the pip does not sit on the ring it is drawn on");
                Assert.AreEqual(pip.ScreenRadius, pip.DocumentRadius * view.PositionScale, 0.01f,
                    "what is drawn and what answers a click are two different sizes");
            }
        }

        /// <summary>
        /// The fan opens into the WIDEST gap between the node's own branches. The tree is radial, so the
        /// way away from the wheel's middle is exactly where a node's children hang: a ring aimed there
        /// draws its pips on top of the very lines it is meant to sit beside.
        /// <para>Three branches at a quarter, a half and none of a turn leave one gap of half a turn and
        /// two of a quarter, so the fan has one place to go and room to spare on both sides of it.</para>
        /// </summary>
        [TestMethod]
        public void TheFanOpensIntoTheWidestGapBetweenTheBranches()
        {
            const float widestGap = 180f;
            const float gapMiddle = 270f;

            // What the fan has to spare inside the gap, on either side of it.
            const float clearance = (widestGap - ArcDegrees) * 0.5f;

            SocketRingGeometry geometry = Rings();
            PassiveTreeDocument document = Branching("branch", Branches);
            PassiveNode node = document.Find(RingNode)!;
            CanvasTransform view = View();
            SocketRingFan fan = geometry.Fan(document, node, PipsPerRing);

            Assert.AreEqual(gapMiddle, Degrees(fan.CentreRadians), ToleranceDegrees,
                "the fan is not centred in the gap between the branches");

            Assert.AreEqual(ArcDegrees, InDegrees(fan.ArcRadians), ToleranceDegrees,
                "a gap with room to spare still narrowed the fan");

            for (int index = 0; index < PipsPerRing; index++)
            {
                float direction = DirectionOf(geometry.Place(fan, node, view, index, PipsPerRing));

                foreach (float branch in Branches)
                    Assert.IsTrue(Apart(direction, branch) >= clearance - ToleranceDegrees,
                        $"pip {index} sits {Apart(direction, branch):0.#} degrees off the branch leaving at {branch}");
            }
        }

        /// <summary>
        /// A gap too narrow for the authored fan takes a narrower fan rather than all of it: the pips are
        /// pulled in until the outermost clears the branch beside it. The squeeze stops where neighbouring
        /// pips would begin to touch — pips drawn over each other say less than a pip a branch clips.
        /// </summary>
        [TestMethod]
        public void AFanTooWideForItsGapIsSqueezedIntoIt()
        {
            // Four branches a quarter turn apart: every gap is narrower than the authored fan.
            SocketRingGeometry geometry = Rings();
            PassiveTreeDocument document = Branching("branch", 0f, 90f, 180f, 270f);
            PassiveNode node = document.Find(RingNode)!;
            CanvasTransform view = View();
            SocketRingFan fan = geometry.Fan(document, node, PipsPerRing);

            Assert.IsTrue(InDegrees(fan.ArcRadians) < ArcDegrees,
                $"the fan kept its full {ArcDegrees} degrees inside a gap of 90");

            SocketPipPlacement first = geometry.Place(fan, node, view, 0, PipsPerRing);
            SocketPipPlacement second = geometry.Place(fan, node, view, 1, PipsPerRing);

            float between = MathF.Sqrt(
                (second.X - first.X) * (second.X - first.X) + (second.Y - first.Y) * (second.Y - first.Y));

            Assert.IsTrue(between >= first.DocumentRadius + second.DocumentRadius,
                "the squeeze went on until the pips overlapped, which says less than a clipped pip does");
        }

        /// <summary>The commonest node of the tree: one branch in, nothing out. Its ring opens straight
        /// back at the branch, since the whole rest of the turn is free.</summary>
        [TestMethod]
        public void ANodeWithOneBranchOpensItsFanStraightBackAtIt()
        {
            Assert.AreEqual(180f, CentreOf(Rings(), Branching("branch", 0f)), ToleranceDegrees,
                "the fan opened somewhere other than away from the only branch there is");
        }

        /// <summary>A node nothing leaves has no gap to choose, so its ring keeps the plain direction:
        /// away from the middle of the wheel.</summary>
        [TestMethod]
        public void ANodeWithNoBranchesKeepsTheFanFacingAwayFromTheMiddle()
        {
            SocketRingGeometry geometry = Rings();
            PassiveTreeDocument document = Branching("branch");
            PassiveNode node = document.Find(RingNode)!;
            float outward = Degrees(MathF.Atan2(RingNodeY, RingNodeX));

            Assert.AreEqual(outward, CentreOf(geometry, document), ToleranceDegrees,
                "the middle of the fan left the direction away from the wheel's centre");

            Assert.AreEqual(outward,
                DirectionOf(geometry.Place(geometry.Fan(document, node, 1), node, View(), 0, 1)), ToleranceDegrees,
                "a lone pip left the fan's own direction");
        }

        /// <summary>
        /// Two gaps of one width settle by geometry, never by the graph. Neighbours are held in a hash
        /// set, whose order differs between runs and between the names the nodes happen to carry, so a
        /// stance seed — four branches, two equal gaps — would swap the side of its ring between runs.
        /// </summary>
        [TestMethod]
        public void TwoGapsOfOneWidthPickTheSameSideWhateverTheGraphIsCalled()
        {
            // The shape of a stance seed on the shipped tree, and the middle of the gap opening nearest
            // the way away from the wheel — where the ring sat before there were branches to avoid.
            float[] branches = [0f, 85.6f, 180f, 265.6f];
            const float nearerOutward = 312.8f;

            SocketRingGeometry geometry = Rings();

            float[] reversed = [.. Enumerable.Reverse(branches)];

            // Walked under several names on purpose: the hash order of the neighbour set follows the ids,
            // so one name alone could pass by luck where the rule had been dropped.
            foreach (string names in new[] { "branch", "spoke", "arm", "ray", "limb" })
            {
                Assert.AreEqual(nearerOutward, CentreOf(geometry, Branching(names, branches)), ToleranceDegrees,
                    $"the ring of a symmetric node went the wrong way when its branches were called {names}");

                Assert.AreEqual(nearerOutward, CentreOf(geometry, Branching(names, reversed)), ToleranceDegrees,
                    $"the ring of a symmetric node moved when its branches were linked the other way round");
            }
        }

        /// <summary>Which nodes open a slot OF THEIR OWN, and so have a tier and an ability to name in
        /// their popup. The node that hands the ability over brings the tier-one slot with it and lists
        /// the whole ring beside itself, and a content node opens nothing at all.</summary>
        [TestMethod]
        public void OnlyASocketNodeOpensASlotOfItsOwn()
        {
            Assert.IsTrue(NodeKindRules.OpensOwnSlot(PassiveNodeKind.SocketTier2));
            Assert.IsTrue(NodeKindRules.OpensOwnSlot(PassiveNodeKind.SocketTier3));

            Assert.AreEqual(2, NodeKindRules.SocketTier(PassiveNodeKind.SocketTier2));
            Assert.AreEqual(3, NodeKindRules.SocketTier(PassiveNodeKind.SocketTier3));

            Assert.IsFalse(NodeKindRules.OpensOwnSlot(PassiveNodeKind.AbilityUnlock),
                "the ability's own node would say a second time what its ring already lists");

            Assert.IsFalse(NodeKindRules.OpensOwnSlot(PassiveNodeKind.Start), "a stance seed opens a slot of its own");
            Assert.IsFalse(NodeKindRules.OpensOwnSlot(PassiveNodeKind.Small));
            Assert.IsFalse(NodeKindRules.OpensOwnSlot(PassiveNodeKind.Notable));
            Assert.IsFalse(NodeKindRules.OpensOwnSlot(PassiveNodeKind.Keystone));
        }

        /// <summary>A node too small on screen wears no ring at all — a mark on a dot a pixel and a half
        /// across has stopped being that dot's mark — and the rule is one, so the pick agrees.</summary>
        [TestMethod]
        public void ARingStopsBeingDrawnBeforeItStopsBeingAimable()
        {
            var geometry = new SocketRingGeometry(Geometry(), RingScale, PipScale, ArcDegrees, 2f, 6f);

            Assert.IsTrue(geometry.Draws(PassiveNodeKind.AbilityUnlock, 1f));
            Assert.IsFalse(geometry.Draws(PassiveNodeKind.AbilityUnlock, CanvasTransform.MinZoom),
                "the whole tree on screen still drew rings on dots nobody can aim at");
        }

        /// <summary>An ability node away from the middle of the wheel with a branch leaving in each of the
        /// given directions — the two facts a ring's placement is read off. The branches are named after
        /// <paramref name="names"/> because the order the graph hands neighbours over follows their ids,
        /// and no reading here may depend on it.</summary>
        private static PassiveTreeDocument Branching(string names, params float[] degrees)
        {
            var document = new PassiveTreeDocument();
            document.AddNode(new PassiveNode
            {
                Id = RingNode,
                Kind = PassiveNodeKind.AbilityUnlock,
                AbilityId = Ability,
                X = RingNodeX,
                Y = RingNodeY
            });

            for (int index = 0; index < degrees.Length; index++)
            {
                float radians = degrees[index] * MathF.PI / 180f;
                string id = $"{names}_{index}";

                document.AddNode(new PassiveNode
                {
                    Id = id,
                    Kind = PassiveNodeKind.Small,
                    X = RingNodeX + MathF.Cos(radians) * BranchLength,
                    Y = RingNodeY + MathF.Sin(radians) * BranchLength
                });

                document.Link(RingNode, id);
            }

            return document;
        }

        /// <summary>The ring geometry at the shape the game's resource carries.</summary>
        private static SocketRingGeometry Rings() =>
            new(Geometry(), RingScale, PipScale, ArcDegrees, 2f, 6f);

        /// <summary>Which way the node's fan opens, in degrees of a turn.</summary>
        private static float CentreOf(SocketRingGeometry geometry, PassiveTreeDocument document) =>
            Degrees(geometry.Fan(document, document.Find(RingNode)!, PipsPerRing).CentreRadians);

        /// <summary>A view with a spread, a zoom and a pan on it, so nothing under test can pass by
        /// reading a scale of one.</summary>
        private static CanvasTransform View()
        {
            var view = new CanvasTransform();
            view.SetSpread(2.25f, 0f, 0f);
            view.ZoomBy(2f, 0f, 0f);
            view.SetPan(640f, 360f);

            return view;
        }

        /// <summary>Which way the pip sits out of its node, in degrees of a turn.</summary>
        private static float DirectionOf(SocketPipPlacement pip) =>
            Degrees(MathF.Atan2(pip.Y - RingNodeY, pip.X - RingNodeX));

        /// <summary>An angle in degrees, at whatever it measures — a direction or a width.</summary>
        private static float InDegrees(float radians) => radians * 180f / MathF.PI;

        /// <summary>A direction read as a turn in one direction: none to a whole turn.</summary>
        private static float Degrees(float radians)
        {
            float degrees = InDegrees(radians) % 360f;

            return degrees < 0f ? degrees + 360f : degrees;
        }

        /// <summary>How far apart two directions are, the short way round.</summary>
        private static float Apart(float first, float second)
        {
            float delta = MathF.Abs(first - second) % 360f;

            return delta > 180f ? 360f - delta : delta;
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

namespace LastBreathTest.PassiveTree
{
    using Battle.Source.UIElements.PassiveWheel;
    using Core.Battle.Abilities;
    using Core.Enums;
    using Core.PassiveTree;
    using Core.PassiveTree.View;

    /// <summary>
    /// What the left button means when the point under it belongs to two things at once — a node of the
    /// wheel and a pip of that node's ring.
    ///
    /// <para>The pip of an OPEN slot answers for itself: it is the one state with a question to ask of
    /// the bag. The other three leave the click where it always went, to the node, and that split is the
    /// whole of the change — a pip that claimed every click would plan to give an ability back every
    /// time the player asked what fits into one of its slots while a return was being drawn up.</para>
    ///
    /// <para>The address the claimed pip carries is the BOARD's own address for that slot, not a second
    /// spelling of one: the picker seats through the same gate a dragged copy is dropped into, and two
    /// readings of "which slot is this" would be two slots.</para>
    /// </summary>
    [TestClass]
    public class PassiveWheelPipClickTests
    {
        private const string Ability = "Ability_Dex";
        private const string Unlock = "unlock";
        private const string SecondSlot = "socket_two";
        private const string ThirdSlot = "socket_three";

        /// <summary>Ring and pip sizes as multiples of the node's own radius — the shape of the game's
        /// resource rather than its numbers, which live in a .tres and move with the art.</summary>
        private const float RingScale = 2.1f;

        private const float PipScale = 0.55f;
        private const float ArcDegrees = 120f;

        /// <summary>How far outside a pip a point is pushed when the case is about the pip NOT answering.
        /// Any positive slack would do: the rule has none of its own.</summary>
        private const float JustOutsidePixels = 2f;

        /// <summary>The gap the fixture's two branches leave between them, and what a fan of the authored
        /// width has to spare inside it — half the difference on either side.</summary>
        private const float WidestGapDegrees = 270f;

        private const float LeastClearanceDegrees = (WidestGapDegrees - ArcDegrees) * 0.5f;

        /// <summary>Slack on an angle: the outermost pip lands exactly on the clearance, and the
        /// arithmetic that puts it there is single-precision trigonometry.</summary>
        private const float ToleranceDegrees = 0.1f;

        /// <summary>
        /// Only the pip of an open, empty slot takes the left button for itself. Walked over one ring
        /// holding all four states in turn, because they are four readings of ONE board and a fixture per
        /// state would let them drift apart.
        /// </summary>
        [TestMethod]
        public void OnlyTheOpenSlotsPipTakesTheLeftClickForItself()
        {
            var bench = new Bench();
            bench.Open(Unlock, NodeKindRules.UnlockSocketTier);
            bench.Open(SecondSlot, tier: 2);
            bench.Seat(SecondSlot, tier: 2);

            Assert.AreEqual(WheelClickTarget.Slot, WheelHit.LeftClickOn(bench.Pip(Unlock)),
                "an open slot's pip left the click to its node, and the player has no way to ask what fits");

            Assert.AreEqual(WheelClickTarget.Node, WheelHit.LeftClickOn(bench.Pip(SecondSlot)),
                "a filled pip claimed a click it has nothing to offer for — the right button empties it");

            Assert.AreEqual(WheelClickTarget.Node, WheelHit.LeftClickOn(bench.Pip(ThirdSlot)),
                "a pip whose node nobody has bought claimed the click");

            Assert.AreEqual(WheelClickTarget.Node, WheelHit.LeftClickOn(null),
                "a point on no pip at all stopped being a click on its node");

            // The node behind the filled slot goes back: the slot closes around the player's augment and
            // becomes remove-only, which is still not a question the bag can answer.
            bench.Close(SecondSlot);
            Assert.AreEqual(SocketSlotState.Held, bench.Pip(SecondSlot).State, "the fixture did not close the slot");
            Assert.AreEqual(WheelClickTarget.Node, WheelHit.LeftClickOn(bench.Pip(SecondSlot)),
                "a closed pip offered to fill a slot that only opens outwards");
        }

        /// <summary>
        /// A pip answers inside its own circle and nowhere else. Its priority over the node rule is the
        /// whole reason it has no slack: the node rule keeps the slack that makes a dot aimable with the
        /// tree framed, and a pip carrying slack of its own would eat clicks aimed past it.
        /// </summary>
        [TestMethod]
        public void APipIsHitInsideItsOwnCircleAndNowhereElse()
        {
            var bench = new Bench();
            bench.Open(Unlock, NodeKindRules.UnlockSocketTier);
            bench.Open(SecondSlot, tier: 2);
            IReadOnlyList<PassiveSocketPip> pips = bench.Pips();
            Assert.AreEqual(3, pips.Count, "the fixture did not draw the ring the rest of the case is about");

            foreach (PassiveSocketPip pip in pips)
            {
                float centreX = bench.View.ScreenX(pip.X);
                float centreY = bench.View.ScreenY(pip.Y);

                Assert.AreEqual(pip.Address, WheelHit.PipAt(pips, bench.View, centreX, centreY)?.Address,
                    "a point on a pip did not answer with that pip");

                // Pushed straight out along the ring: every other pip sits on the smaller circle, so this
                // leaves one pip without landing in another.
                float nodeX = bench.View.ScreenX(bench.Node().X);
                float nodeY = bench.View.ScreenY(bench.Node().Y);
                float reach = MathF.Sqrt((centreX - nodeX) * (centreX - nodeX) + (centreY - nodeY) * (centreY - nodeY));
                float step = pip.ScreenRadius + JustOutsidePixels;

                Assert.IsNull(
                    WheelHit.PipAt(pips, bench.View,
                        centreX + (centreX - nodeX) / reach * step,
                        centreY + (centreY - nodeY) / reach * step),
                    "a pip answered a point outside itself, and the node rule never got the click");
            }

            Assert.IsNull(WheelHit.PipAt(pips, bench.View, bench.View.ScreenX(bench.Node().X), bench.View.ScreenY(bench.Node().Y)),
                "the node's own centre answered with a pip");
        }

        /// <summary>
        /// The ring is laid clear of the node's own branches, and the pick moves with it: every pip
        /// answers at the very place it was drawn. Drawing and picking read ONE layout, so what protects
        /// them is walking that layout where the branches have pushed it to — a fan aimed away from the
        /// wheel's middle, as it used to be, would put pips on the lines the edge layer draws.
        /// </summary>
        [TestMethod]
        public void TheFanLaidClearOfTheBranchesIsPickedWhereItIsDrawn()
        {
            var bench = new Bench();
            bench.Open(Unlock, NodeKindRules.UnlockSocketTier);
            bench.Open(SecondSlot, tier: 2);

            IReadOnlyList<PassiveSocketPip> pips = bench.Pips();
            Assert.AreEqual(3, pips.Count, "the fixture did not draw the ring the case is about");

            foreach (PassiveSocketPip pip in pips)
            {
                foreach (float branch in Bench.BranchDegrees)
                    Assert.IsTrue(bench.Apart(pip, branch) >= LeastClearanceDegrees - ToleranceDegrees,
                        $"a pip sits {bench.Apart(pip, branch):0.#} degrees off the branch leaving at {branch}, where the edge is drawn");

                Assert.AreEqual(pip.Address,
                    WheelHit.PipAt(pips, bench.View, bench.View.ScreenX(pip.X), bench.View.ScreenY(pip.Y))?.Address,
                    "a pip moved off the branches stopped answering where it is drawn");
            }
        }

        /// <summary>
        /// The address a claimed pip hands the courier is the one the board knows the slot by — the same
        /// one a dropped copy is judged against. Read off the board rather than spelled out here, since
        /// what is being protected is that there is ONE spelling.
        /// </summary>
        [TestMethod]
        public void TheClaimedPipNamesTheSlotTheBoardKnows()
        {
            var bench = new Bench();
            bench.Open(Unlock, NodeKindRules.UnlockSocketTier);
            bench.Open(SecondSlot, tier: 2);

            PassiveSocketPip pip = bench.Pip(Unlock);
            Assert.AreEqual(WheelClickTarget.Slot, WheelHit.LeftClickOn(pip), "the case is about a pip that claims the click");

            AbilitySocket? socket = bench.Board.Find(pip.Address);
            Assert.IsNotNull(socket, "the picker would be opened for an address the board has never heard of");
            Assert.IsTrue(socket.IsOpen && socket.IsEmpty, "the offer would be made for a slot that cannot take anything");

            CollectionAssert.Contains(
                bench.Board.SocketsOf(Ability).Select(entry => entry.Address).ToArray(), pip.Address,
                "the pip names a slot that is not one of the ability's own");

            Assert.AreEqual(Unlock, pip.OpenerId, "the wheel would check its own refusals against the wrong node");

            // Every pip of the ring, claimed or not, carries the address of the slot it was collected
            // from: the pips and the board's slots line up one to one.
            CollectionAssert.AreEquivalent(
                bench.RingAddresses().ToArray(),
                bench.Pips().Select(entry => entry.Address).ToArray(),
                "the ring drawn and the ring collected are not the same slots");
        }

        /// <summary>A document, the board over it, and the ring of pips the wheel would draw — built the
        /// way the socket layer builds it, off the document for composition and off the board for state.
        /// The layer additionally skips rings of nodes the character does not hold; the one node here is
        /// held throughout, so the filter has nothing to say about these walks.</summary>
        private sealed class Bench
        {
            private readonly PassiveTreeDocument _document = Tree();
            private readonly SocketRingGeometry _geometry;
            private readonly List<AbilitySocketPlacement> _open = [];
            private readonly Dictionary<string, List<SocketRingSlot>> _rings = [];

            internal Bench()
            {
                var nodes = new NodeGeometry(Radii(), 1.5f, 6f);
                _geometry = new SocketRingGeometry(nodes, RingScale, PipScale, ArcDegrees, 2f, 6f);

                View = new CanvasTransform();
                View.SetSpread(2.25f, 0f, 0f);
                View.ZoomBy(2f, 0f, 0f);
                View.SetPan(640f, 360f);

                Board = new AbilitySocketBoard();
                SocketRings.Collect(_document, _rings);
            }

            /// <summary>Where the node's own branches leave, which is what the fan has to be laid clear
            /// of. Read here rather than recomputed by the cases: the tree below is what puts them
            /// there.</summary>
            internal static IReadOnlyList<float> BranchDegrees => [0f, 270f];

            internal AbilitySocketBoard Board { get; }

            internal CanvasTransform View { get; }

            /// <summary>How far a pip sits from a direction out of the node, the short way round.</summary>
            internal float Apart(PassiveSocketPip pip, float degrees)
            {
                PassiveNode node = Node();
                float direction = MathF.Atan2(pip.Y - node.Y, pip.X - node.X) * 180f / MathF.PI;
                float delta = MathF.Abs(direction - degrees) % 360f;

                return delta > 180f ? 360f - delta : delta;
            }

            /// <summary>Adds a slot to what the allocation opens and syncs the board to it.</summary>
            internal void Open(string socketId, int tier)
            {
                _open.Add(new AbilitySocketPlacement(socketId, Ability, tier));
                Board.Sync(_open);
            }

            internal void Seat(string socketId, int tier) =>
                Assert.IsTrue(
                    Board.Install(Address(socketId, tier),
                        new AugmentInstance("Augment_Any", new Dictionary<string, float>(), Rarity.Common)),
                    $"the fixture could not fill {socketId}");

            /// <summary>Takes the node behind a slot back out of the allocation. A slot holding something
            /// closes around it; an empty one simply goes.</summary>
            internal void Close(string socketId)
            {
                _open.RemoveAll(placement => placement.SocketId == socketId);
                Board.Sync(_open);
            }

            internal PassiveNode Node() => _document.Find(Unlock)!;

            /// <summary>The pip standing for the slot that node opens.</summary>
            internal PassiveSocketPip Pip(string openerId) =>
                Pips().Single(pip => pip.OpenerId == openerId);

            internal IReadOnlyList<string> RingAddresses() => [.. _rings[Unlock].Select(slot => slot.Address)];

            /// <summary>The ring as the socket layer lays it out: composition from the document, state
            /// from the board, placement from the one geometry both drawing and picking read.</summary>
            internal IReadOnlyList<PassiveSocketPip> Pips()
            {
                PassiveNode node = Node();
                List<SocketRingSlot> ring = _rings[Unlock];
                List<PassiveSocketPip> pips = [];
                SocketRingFan fan = _geometry.Fan(_document, node, ring.Count);

                for (int index = 0; index < ring.Count; index++)
                {
                    SocketPipPlacement placement =
                        _geometry.Place(fan, node, View, index, ring.Count);

                    pips.Add(new PassiveSocketPip(
                        ring[index].Address, node.Id, ring[index].OpenerId,
                        SocketRings.StateOf(Board.Find(ring[index].Address)),
                        placement.X, placement.Y, placement.DocumentRadius, placement.ScreenRadius));
                }

                return pips;
            }

            private string Address(string socketId, int tier) =>
                new AbilitySocketPlacement(socketId, Ability, tier).Address;

            private static Dictionary<PassiveNodeKind, float> Radii()
            {
                var radii = new Dictionary<PassiveNodeKind, float>();
                float radius = 5f;
                foreach (PassiveNodeKind kind in Enum.GetValues<PassiveNodeKind>()) radii[kind] = radius++;

                return radii;
            }

            /// <summary>An ability node away from the middle of the wheel, with the two socket nodes of
            /// its own ability hanging off it on branches — so the ring has lines to be laid clear of,
            /// which is what half the walks below are about. One of the two is never bought.</summary>
            private static PassiveTreeDocument Tree()
            {
                const float nodeX = 120f;
                const float nodeY = -80f;
                const float branch = 100f;

                var document = new PassiveTreeDocument();
                document.AddNode(new PassiveNode
                {
                    Id = Unlock,
                    Kind = PassiveNodeKind.AbilityUnlock,
                    AbilityId = Ability,
                    X = nodeX,
                    Y = nodeY
                });

                Branch(SecondSlot, PassiveNodeKind.SocketTier2, nodeX + branch, nodeY);
                Branch(ThirdSlot, PassiveNodeKind.SocketTier3, nodeX, nodeY - branch);

                return document;

                void Branch(string id, PassiveNodeKind kind, float x, float y)
                {
                    document.AddNode(new PassiveNode { Id = id, Kind = kind, AbilityId = Ability, X = x, Y = y });
                    document.Link(Unlock, id);
                }
            }
        }
    }
}

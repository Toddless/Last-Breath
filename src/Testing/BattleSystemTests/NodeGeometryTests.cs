namespace LastBreathTest.BattleSystemTests
{
    using Core.PassiveTree;
    using Core.PassiveTree.View;

    /// <summary>
    /// One rule for what a click landed on, and one number behind both the size a node is drawn at and
    /// the size it is picked at. The wheel's layers, its node scenes and its picker are three readings
    /// of the same radius, so the failure this class exists to prevent is a node whose visible dot is
    /// bigger than the area that answers the mouse.
    /// </summary>
    [TestClass]
    public class NodeGeometryTests
    {
        private const float MinScreenRadius = 1.5f;
        private const float PickSlack = 6f;
        private const float Tolerance = 0.001f;

        /// <summary>The two rings the wheel draws around a node — what can be bought next, and what the
        /// pointer is on. Copies of the look's numbers on purpose: what is under test is the RULE that
        /// a ring is measured off the node, not the two figures an artist may retune.</summary>
        private const float FrontierRing = 1.3f;

        private const float HoverRing = 1.55f;

        /// <summary>Past this a mark is no longer that node's mark. A ring twice the dot it is about is
        /// read as a second object sitting beside it, which is exactly what a gap held in pixels turns
        /// into once the floor takes over the node's own size.</summary>
        private const float MaxRingRatio = 2f;

        /// <summary>The layout has around a hundred and fifty pairs of shapes physically overlapping, so
        /// which of two nodes a click means is decided by distance and never by the order the author
        /// happened to type them into the file — an order he rewrites daily.</summary>
        [TestMethod]
        public void WhereTwoNodesOverlap_TheNearestCentreWins_WhicheverWasAuthoredFirst()
        {
            const float near = 100f;
            const float far = 112f;

            AssertPicksNearest(Tree(("near", near), ("far", far)));
            AssertPicksNearest(Tree(("far", far), ("near", near)));
        }

        /// <summary>Aimed near enough that the spatial grid still offers the node as a candidate, and far
        /// enough that its radius refuses it — the gap between "what is worth measuring" and "what was
        /// actually hit".</summary>
        [TestMethod]
        public void APointOutsideEveryNode_BelongsToNoNode()
        {
            const float justOutside = 40f;
            NodeGeometry geometry = Geometry();
            PassiveTreeDocument document = Tree(("only", 0f));
            var view = new CanvasTransform();

            Assert.IsTrue(justOutside > geometry.PickRadius(PassiveNodeKind.Notable, view.Zoom),
                "the fixture must aim outside the node it is about");
            Assert.IsNull(geometry.At(document, view, justOutside, 0f));
        }

        /// <summary>The floor under a drawn node is the floor under its clickable area too. Split apart
        /// they drift, and the drift is invisible until somebody misses a dot he can plainly see.</summary>
        [TestMethod]
        public void ADrawnNodeIsClickableAtEveryZoom()
        {
            NodeGeometry geometry = Geometry();

            foreach (float zoom in Zooms())
                foreach (PassiveNodeKind kind in Enum.GetValues<PassiveNodeKind>())
                {
                    float drawn = geometry.DocumentRadius(kind, zoom) * zoom;
                    Assert.IsTrue(geometry.PickRadius(kind, zoom) >= drawn,
                        $"{kind} at zoom {zoom} is drawn larger than the area that answers a click");
                }
        }

        /// <summary>A node scene lives inside the scaled frame, so the counter-scale is what keeps it the
        /// size the floor promised. Never below 1: shrinking a scene under its authored size is what the
        /// zoom already does.</summary>
        [TestMethod]
        public void TheCounterScale_KeepsASceneAtTheSizeTheFloorPromised()
        {
            NodeGeometry geometry = Geometry();

            foreach (float zoom in Zooms())
                foreach (PassiveNodeKind kind in Enum.GetValues<PassiveNodeKind>())
                {
                    float onScreen = geometry.AuthoredRadius(kind) * zoom * geometry.ViewScale(kind, zoom);
                    Assert.AreEqual(geometry.ScreenRadius(kind, zoom), onScreen, Tolerance, $"{kind} at zoom {zoom}");
                    Assert.IsTrue(geometry.ViewScale(kind, zoom) >= 1f - Tolerance, $"{kind} at zoom {zoom}");
                }
        }

        /// <summary>A mark around a node is a proportion of that node and stays that proportion at every
        /// zoom. Held as a gap in pixels instead, it drifts: the node stops shrinking at its floor while
        /// the gap keeps its distance, and the same ring is a hair at one end of the range and a halo at
        /// the other.</summary>
        [TestMethod]
        public void AMarkAroundANode_IsTheSameProportionOfItAtEveryZoom()
        {
            NodeGeometry geometry = Geometry();

            foreach (float ring in Rings())
                foreach (PassiveNodeKind kind in Enum.GetValues<PassiveNodeKind>())
                {
                    float reference = Ratio(geometry, kind, 1f, ring);

                    foreach (float zoom in Zooms())
                        Assert.AreEqual(reference, Ratio(geometry, kind, zoom, ring), Tolerance,
                            $"the ring {ring} around {kind} drifted away from the node at zoom {zoom}");
                }
        }

        /// <summary>The floor holds the dot at a pixel and a half and holds nothing else, so this is
        /// where a mark measured in anything but the node's own size comes off it. What the owner saw:
        /// a ring several times the node it was about, read as an object of its own.</summary>
        [TestMethod]
        public void WhereTheFloorHoldsTheDot_TheMarkStaysOnIt()
        {
            NodeGeometry geometry = Geometry();

            foreach (float ring in Rings())
                foreach (PassiveNodeKind kind in Enum.GetValues<PassiveNodeKind>())
                {
                    Assert.AreEqual(MinScreenRadius, geometry.ScreenRadius(kind, CanvasTransform.MinZoom), Tolerance,
                        $"the fixture must be pulled out far enough for the floor to hold {kind}");

                    float ratio = Ratio(geometry, kind, CanvasTransform.MinZoom, ring);
                    Assert.IsTrue(ratio > 1f, $"the ring {ring} around {kind} is drawn inside the node");
                    Assert.IsTrue(ratio <= MaxRingRatio,
                        $"the ring {ring} around {kind} is {ratio:0.00} times the dot it marks");
                }
        }

        /// <summary>A ring is drawn around a node, never through it: a look that asked for something
        /// smaller than the node would otherwise mark it with a line across its face.</summary>
        [TestMethod]
        public void AMarkSmallerThanTheNode_IsStillDrawnAroundIt()
        {
            NodeGeometry geometry = Geometry();

            foreach (float zoom in Zooms())
                foreach (PassiveNodeKind kind in Enum.GetValues<PassiveNodeKind>())
                    Assert.IsTrue(geometry.ScreenRingRadius(kind, zoom, 0.5f) >= geometry.ScreenRadius(kind, zoom),
                        $"{kind} at zoom {zoom}");
        }

        /// <summary>The ends of the working range, written down. Everything above the floor band is the
        /// authored size taken straight, everything at the bottom of it is the floor and nothing else —
        /// the two answers a redraw per wheel click is not allowed to change.</summary>
        [TestMethod]
        public void TheSizesAtBothEndsOfTheRange_AreTheAuthoredOneAndTheFloor()
        {
            NodeGeometry geometry = Geometry();

            foreach (PassiveNodeKind kind in Enum.GetValues<PassiveNodeKind>())
            {
                float authored = geometry.AuthoredRadius(kind);

                Assert.AreEqual(authored * CanvasTransform.MaxZoom, geometry.ScreenRadius(kind, CanvasTransform.MaxZoom),
                    Tolerance, $"{kind} is held up by a floor where nothing should be");
                Assert.AreEqual(1f, geometry.ViewScale(kind, CanvasTransform.MaxZoom), Tolerance);

                Assert.AreEqual(MinScreenRadius, geometry.ScreenRadius(kind, CanvasTransform.MinZoom), Tolerance);
                Assert.IsTrue(geometry.ViewScale(kind, CanvasTransform.MinZoom) > 1f,
                    $"{kind} draws smaller than the floor it was promised");

                foreach (float zoom in Zooms())
                    Assert.AreEqual(geometry.ScreenRadius(kind, zoom), geometry.DocumentRadius(kind, zoom) * zoom,
                        Tolerance, $"{kind} at zoom {zoom} is handed to the frame as another size");
            }
        }

        [TestMethod]
        public void AClassWithNoAuthoredRadius_FailsWhereItIsBuilt()
        {
            var partial = new Dictionary<PassiveNodeKind, float> { [PassiveNodeKind.Small] = 5f };

            Assert.ThrowsException<InvalidOperationException>(() => new NodeGeometry(partial, MinScreenRadius, PickSlack));
        }

        private static void AssertPicksNearest(PassiveTreeDocument document)
        {
            NodeGeometry geometry = Geometry();
            var view = new CanvasTransform();

            PassiveNode? picked = geometry.At(document, view, 104f, 0f);

            Assert.IsNotNull(picked);
            Assert.AreEqual("near", picked.Id, "the pick followed the order of the file instead of the distance");
        }

        /// <summary>Two nodes on one line, close enough that the point tested falls inside both.</summary>
        private static PassiveTreeDocument Tree(params (string Id, float X)[] nodes)
        {
            var document = new PassiveTreeDocument();
            foreach ((string id, float x) in nodes)
                document.AddNode(new PassiveNode { Id = id, Kind = PassiveNodeKind.Notable, X = x });

            return document;
        }

        private static NodeGeometry Geometry()
        {
            var radii = new Dictionary<PassiveNodeKind, float>();
            float radius = 5f;
            foreach (PassiveNodeKind kind in Enum.GetValues<PassiveNodeKind>()) radii[kind] = radius++;

            return new NodeGeometry(radii, MinScreenRadius, PickSlack);
        }

        private static float[] Zooms() => [CanvasTransform.MinZoom, 0.2f, 0.55f, 1f, 2f, CanvasTransform.MaxZoom];

        private static float[] Rings() => [FrontierRing, HoverRing];

        /// <summary>How many times the node its own mark measures — the number the eye actually judges,
        /// and the only reading either ring is worth testing in.</summary>
        private static float Ratio(NodeGeometry geometry, PassiveNodeKind kind, float zoom, float ring) =>
            geometry.ScreenRingRadius(kind, zoom, ring) / geometry.ScreenRadius(kind, zoom);
    }
}

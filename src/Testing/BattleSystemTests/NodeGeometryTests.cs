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
        /// they drift, and the drift is invisible until somebody misses a dot he can plainly see. Walked
        /// at every spread as well: what a size is divided by on its way into the frame is the frame's
        /// own scale, and dividing by the zoom instead is right only while the spread is one.</summary>
        [TestMethod]
        public void ADrawnNodeIsClickableAtEveryZoomAndEverySpread()
        {
            NodeGeometry geometry = Geometry();

            foreach (CanvasTransform view in Views())
                foreach (PassiveNodeKind kind in Enum.GetValues<PassiveNodeKind>())
                {
                    float drawn = geometry.DocumentRadius(kind, view) * view.Frame().Scale;
                    Assert.IsTrue(geometry.PickRadius(kind, view.Zoom) >= drawn,
                        $"{kind} at zoom {view.Zoom} spread {view.Spread} is drawn larger than the area that answers a click");
                }
        }

        /// <summary>A node scene lives inside the scaled frame, so the counter-scale is what keeps it the
        /// size the floor promised — at every spread, since the frame carries the spread too. Never below
        /// 1 while the layout is drawn at its authored spread: shrinking a scene under its authored size
        /// is what the zoom already does.</summary>
        [TestMethod]
        public void TheCounterScale_KeepsASceneAtTheSizeTheFloorPromised()
        {
            NodeGeometry geometry = Geometry();

            foreach (CanvasTransform view in Views())
                foreach (PassiveNodeKind kind in Enum.GetValues<PassiveNodeKind>())
                {
                    string at = $"{kind} at zoom {view.Zoom} spread {view.Spread}";
                    float onScreen = geometry.AuthoredRadius(kind) * view.Frame().Scale * geometry.ViewScale(kind, view);

                    Assert.AreEqual(geometry.ScreenRadius(kind, view.Zoom), onScreen, Tolerance, at);

                    if (MathF.Abs(view.Spread - CanvasTransform.DefaultSpread) < Tolerance)
                        Assert.IsTrue(geometry.ViewScale(kind, view) >= 1f - Tolerance, at);
                }
        }

        /// <summary>The drawing and the pick, cross-checked through the very value the canvas assigns to
        /// its frame. The frame lives in an engine node no test here can reach, so the aim point is built
        /// the way the frame builds it — origin plus coordinate times scale — and the picker has to
        /// answer at that pixel. Aiming through the transform's own <c>ScreenX</c> instead would be the
        /// pick checked against itself, which is exactly the check that passed while the frame was
        /// scaled by the zoom and the pick by the zoom times the spread.</summary>
        [TestMethod]
        public void APointTheFrameDrawsANodeAt_IsAPointThePickerAnswers()
        {
            NodeGeometry geometry = Geometry();
            PassiveTreeDocument document = Tree(("only", 40f));
            PassiveNode node = document.Nodes[0];

            foreach (CanvasTransform view in Views())
            {
                view.SetPan(310f, 190f);
                CanvasFrame frame = view.Frame();

                float drawnX = frame.X + node.X * frame.Scale;
                float drawnY = frame.Y + node.Y * frame.Scale;

                Assert.AreEqual(drawnX, view.ScreenX(node.X), Tolerance,
                    $"the frame draws the node somewhere the transform does not place it at spread {view.Spread}");
                Assert.IsNotNull(geometry.At(document, view, drawnX, drawnY),
                    $"the node is drawn at zoom {view.Zoom} spread {view.Spread} where nothing answers a click");
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
            CanvasTransform deepest = View(CanvasTransform.MaxZoom, CanvasTransform.DefaultSpread);
            CanvasTransform widest = View(CanvasTransform.MinZoom, CanvasTransform.DefaultSpread);

            foreach (PassiveNodeKind kind in Enum.GetValues<PassiveNodeKind>())
            {
                float authored = geometry.AuthoredRadius(kind);

                Assert.AreEqual(authored * CanvasTransform.MaxZoom, geometry.ScreenRadius(kind, CanvasTransform.MaxZoom),
                    Tolerance, $"{kind} is held up by a floor where nothing should be");
                Assert.AreEqual(1f, geometry.ViewScale(kind, deepest), Tolerance);

                Assert.AreEqual(MinScreenRadius, geometry.ScreenRadius(kind, CanvasTransform.MinZoom), Tolerance);
                Assert.IsTrue(geometry.ViewScale(kind, widest) > 1f,
                    $"{kind} draws smaller than the floor it was promised");

                foreach (CanvasTransform view in Views())
                    Assert.AreEqual(geometry.ScreenRadius(kind, view.Zoom),
                        geometry.DocumentRadius(kind, view) * view.Frame().Scale, Tolerance,
                        $"{kind} at zoom {view.Zoom} spread {view.Spread} is handed to the frame as another size");
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

        /// <summary>Both ends of the spread range and the two stops in between that matter: the authored
        /// default, and the value the tree is actually laid out at today.</summary>
        private static float[] Spreads() => [CanvasTransform.MinSpread, CanvasTransform.DefaultSpread, 2.25f, CanvasTransform.MaxSpread];

        /// <summary>Every zoom crossed with every spread. A size must come out of the geometry the same
        /// number of pixels across in all of them — that is what the spread means — and the frame's scale
        /// is what carries it there.</summary>
        private static IEnumerable<CanvasTransform> Views()
        {
            foreach (float spread in Spreads())
                foreach (float zoom in Zooms())
                    yield return View(zoom, spread);
        }

        private static CanvasTransform View(float zoom, float spread)
        {
            var view = new CanvasTransform();
            view.SetSpread(spread, 0f, 0f);
            view.ZoomBy(zoom, 0f, 0f);
            return view;
        }

        private static float[] Rings() => [FrontierRing, HoverRing];

        /// <summary>How many times the node its own mark measures — the number the eye actually judges,
        /// and the only reading either ring is worth testing in.</summary>
        private static float Ratio(NodeGeometry geometry, PassiveNodeKind kind, float zoom, float ring) =>
            geometry.ScreenRingRadius(kind, zoom, ring) / geometry.ScreenRadius(kind, zoom);
    }
}

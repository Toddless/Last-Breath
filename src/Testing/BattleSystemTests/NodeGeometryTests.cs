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

        /// <summary>The one thing a zoom step still has to redraw for. Above the band nothing is held up
        /// by a floor and a wheel click costs two field writes.</summary>
        [TestMethod]
        public void TheFloorsAreEngagedOnlyWhereTheSmallestClassIsHeldUp()
        {
            NodeGeometry geometry = Geometry();
            float threshold = MinScreenRadius / geometry.MinAuthoredRadius;

            Assert.IsTrue(geometry.FloorsEngaged(threshold * 0.5f));
            Assert.IsFalse(geometry.FloorsEngaged(threshold * 2f));
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
    }
}

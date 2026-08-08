namespace LastBreathTest.BattleSystemTests
{
    using Core.Data.GameData;
    using Core.PassiveTree;
    using Core.PassiveTree.Allocation;
    using Core.PassiveTree.View;

    /// <summary>
    /// The wheel laid out over the markup the game actually ships — read-only, and without a single
    /// number of it written down here. The tree file is rewritten daily: node ids, counts and the
    /// budget all move, so every assertion is about a PROPERTY of the layout rather than about a
    /// figure.
    /// <para>What is being protected is the one invariant the whole drawing rests on: the nodes that
    /// get a scene and the nodes drawn as part of the mass are exact complements, produced by one
    /// reading of the allocation. Two readings, and a node draws twice or vanishes.</para>
    /// </summary>
    [TestClass]
    public class PassiveWheelLayoutTests
    {
        private const float MinScreenRadius = 1.5f;
        private const float PickSlack = 6f;

        /// <summary>Which classes carry a scene in the game's look. A copy on purpose: the real answer
        /// is a flag in a Godot resource, and what is under test is the SPLIT, not the resource.</summary>
        private static bool HasView(PassiveNodeKind kind) => kind != PassiveNodeKind.Small;

        [TestMethod]
        public void TheTwoNodeLayersAreExactComplements_OverTheShippedMarkup()
        {
            PassiveTreeDocument document = ShippedTree();
            IPassiveTreeService service = ServiceOver(document, points: document.Budget);
            var carriers = new HashSet<string>(StringComparer.Ordinal);

            NodeCarriers.Collect(document, service.TakenNodes, HasView, carriers);

            int field = 0;
            foreach (PassiveNode node in document.Nodes)
                if (!carriers.Contains(node.Id))
                    field++;

            Assert.AreEqual(document.Nodes.Count, carriers.Count + field,
                "a node is drawn either as a scene or as part of the mass — never both, never neither");

            foreach (string id in carriers)
                Assert.IsNotNull(document.Find(id), $"{id} would get a scene with no coordinates to stand at");
        }

        /// <summary>Everything that can animate needs something to animate: a taken node is exactly what
        /// pulses, plays its purchase and wears an augment mark, so taking one has to give it a scene
        /// whatever its class.</summary>
        [TestMethod]
        public void TakingANode_GivesItAScene_EvenWhenItsClassHasNone()
        {
            PassiveTreeDocument document = ShippedTree();
            IPassiveTreeService service = ServiceOver(document, points: document.Budget);
            var carriers = new HashSet<string>(StringComparer.Ordinal);

            string? plain = FirstBuyableOfTheMass(service, document);
            Assert.IsNotNull(plain, "the shipped tree offers no reachable node of a class drawn in the mass");

            NodeCarriers.Collect(document, service.TakenNodes, HasView, carriers);
            Assert.IsFalse(carriers.Contains(plain), "the node was already a carrier before it was taken");

            Assert.AreEqual(AllocationResult.Success, service.Take(plain));

            NodeCarriers.Collect(document, service.TakenNodes, HasView, carriers);
            Assert.IsTrue(carriers.Contains(plain), "a taken node has nothing to animate on");
        }

        /// <summary>
        /// Every node of the shipped markup answers a hand aimed at it, at the zoom the whole wheel is
        /// framed at as well as at the zoom it is read at, and at every spread the layout may be authored
        /// with. Aimed a pixel off centre on purpose: a hand is never exact, and a dot that shrinks below
        /// the floor or loses its slack stops answering long before it stops being visible.
        /// <para>The aim point is built the way the CANVAS FRAME builds it — its origin plus the node's
        /// coordinate times its scale — and not through the transform's own <c>ScreenX</c>. The frame is
        /// an engine node no headless test can reach, so this is the one place the drawing and the pick
        /// are checked against each other; aiming through <c>ScreenX</c> would be the pick checked
        /// against itself and would pass on a frame scaled by the wrong multiplier.</para>
        /// </summary>
        [TestMethod]
        public void EveryShippedNode_AnswersAHandAimedWhereTheFrameDrewIt()
        {
            const float aimSlipPixels = 1f;
            PassiveTreeDocument document = ShippedTree();
            NodeGeometry geometry = ShippedGeometry();

            float[] zooms = [CanvasTransform.MinZoom, 0.3f, 1f, 2f];
            float[] spreads = [CanvasTransform.MinSpread, CanvasTransform.DefaultSpread, 2.25f, CanvasTransform.MaxSpread];

            foreach (float spread in spreads)
                foreach (float zoom in zooms)
                {
                    var view = new CanvasTransform();
                    view.SetSpread(spread, 0f, 0f);
                    view.ZoomBy(zoom, 0f, 0f);
                    view.SetPan(640f, 360f);
                    CanvasFrame frame = view.Frame();

                    foreach (PassiveNode node in document.Nodes)
                    {
                        PassiveNode? picked = geometry.At(document, view,
                            frame.X + node.X * frame.Scale + aimSlipPixels, frame.Y + node.Y * frame.Scale);

                        Assert.IsNotNull(picked, $"{node.Id} answers nothing at zoom {zoom} spread {spread}");
                    }
                }
        }

        /// <summary>Both edge layers walk the document the way they draw it. A link naming a node that
        /// is not there would be a line drawn from nowhere — the layers skip it, and this says whether
        /// the shipped markup has any.</summary>
        [TestMethod]
        public void EveryShippedLink_HasBothOfItsEndsInTheDocument()
        {
            PassiveTreeDocument document = ShippedTree();

            foreach (NodeLink link in document.Links)
            {
                Assert.IsNotNull(document.Find(link.A), $"link {link.A}—{link.B} hangs off nothing");
                Assert.IsNotNull(document.Find(link.B), $"link {link.A}—{link.B} hangs off nothing");
            }
        }

        /// <summary>The wheel prices a route by asking the service and never by walking the graph itself.
        /// Over the real markup that answer has to be a real route: adjacent steps all the way from
        /// something taken to the node under the cursor.</summary>
        [TestMethod]
        public void ThePricedRoute_IsAChainOfAdjacentStepsFromTheAllocation()
        {
            PassiveTreeDocument document = ShippedTree();
            IPassiveTreeService service = ServiceOver(document, points: document.Budget);

            int priced = 0;
            foreach (PassiveNode node in document.Nodes)
            {
                IReadOnlyList<string> route = service.PathTo(node.Id);
                if (route.Count == 0) continue;

                priced++;
                Assert.AreEqual(node.Id, route[^1], "the route does not end at the node it was asked about");

                for (int step = 0; step < route.Count; step++)
                {
                    Assert.IsFalse(service.IsTaken(route[step]), $"{route[step]} is already held and should not be in the price");
                    if (step == 0) continue;

                    Assert.IsTrue(document.AreLinked(route[step - 1], route[step]),
                        $"{route[step - 1]} and {route[step]} are not neighbours");
                }
            }

            Assert.IsTrue(priced > 0, "nothing in the shipped tree could be reached at all");
        }

        private static string? FirstBuyableOfTheMass(IPassiveTreeService service, PassiveTreeDocument document)
        {
            foreach (PassiveNode node in document.Nodes)
                if (!HasView(node.Kind) && service.CheckTake(node.Id) == AllocationResult.Success)
                    return node.Id;

            return null;
        }

        private static NodeGeometry ShippedGeometry()
        {
            var radii = new Dictionary<PassiveNodeKind, float>();
            float radius = 5f;
            foreach (PassiveNodeKind kind in Enum.GetValues<PassiveNodeKind>()) radii[kind] = radius++;

            return new NodeGeometry(radii, MinScreenRadius, PickSlack);
        }

        private static IPassiveTreeService ServiceOver(PassiveTreeDocument document, int points)
        {
            var service = new PassiveTreeService(new FixedTree(document), ConditionCatalogs.Empty());
            service.SetTotalPoints(points);
            return service;
        }

        private static PassiveTreeDocument ShippedTree()
        {
            string path = Path.Combine(SharedData.Catalog(DataCatalog.PassiveTree), PassiveTreeFormat.DefaultFileName);
            List<string> issues = [];
            PassiveTreeDocument document = PassiveTreeSerializer.Deserialize(File.ReadAllText(path), issues);

            Assert.AreEqual(0, issues.Count, string.Join("; ", issues));
            Assert.IsFalse(document.IsEmpty, "the shipped tree read as empty");
            return document;
        }

        private sealed class FixedTree(PassiveTreeDocument tree) : IPassiveTreeProvider
        {
            public PassiveTreeDocument Tree { get; } = tree;

            public IReadOnlyList<string> Issues => [];
        }
    }
}

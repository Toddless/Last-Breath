namespace LastBreathTest.BattleSystemTests
{
    using Core.Enums;
    using Core.PassiveTree;
    using Core.PassiveTree.Allocation;
    using Core.Views.UI;

    /// <summary>
    /// What the wheel is allowed to ask the allocation, and what it is allowed to say when the answer is
    /// no. Buying and giving back go through the service and nowhere else — the window owns no rule of
    /// its own — and a refusal has to arrive carrying the service's own reason, because "not enough
    /// points" and "you have not reached it yet" are two different problems to the player.
    /// <para>Everything here is built on a hand-made tree. The shipped markup's ids move under an
    /// authoring pass, and a test naming one would break on a day nothing was wrong with the code.</para>
    /// </summary>
    [TestClass]
    public class PassiveTreeAllocationSurfaceTests
    {
        private const string Seed = "seed";
        private const string First = "first";
        private const string Second = "second";
        private const string Third = "third";
        private const string Island = "island";

        [TestMethod]
        public void CheckRefund_GivesTheAnswerWithoutGivingAnythingBack()
        {
            IPassiveTreeService service = Service(points: 3);
            service.Take(First);
            service.Take(Second);

            Assert.AreEqual(AllocationResult.WouldOrphan, service.CheckRefund(First), "refunding the middle strands the end");
            Assert.AreEqual(AllocationResult.Success, service.CheckRefund(Second));
            Assert.AreEqual(2, service.SpentPoints, "a check gave a point back");
            Assert.IsTrue(service.IsTaken(Second), "a check took the node off the character");

            Assert.AreEqual(AllocationResult.NotTaken, service.CheckRefund(Third));
            Assert.AreEqual(AllocationResult.UnknownNode, service.CheckRefund("no_such_node"));
            Assert.AreEqual(AllocationResult.Granted, service.CheckRefund(Seed), "a seed is never given back");
        }

        [TestMethod]
        public void PathTo_PricesTheRoute_AndTakePathBuysTheWholeOfIt()
        {
            IPassiveTreeService service = Service(points: 3);

            IReadOnlyList<string> route = service.PathTo(Third);

            CollectionAssert.AreEqual(new[] { First, Second, Third }, route.ToArray(),
                "the route is the cheapest chain of nodes still to be bought");
            Assert.AreEqual(0, service.SpentPoints, "pricing a route spent a point");

            Assert.AreEqual(AllocationResult.Success, service.TakePath(route));
            Assert.AreEqual(3, service.SpentPoints);
            Assert.AreEqual(0, service.PathTo(Third).Count, "a node already held costs nothing to reach");
        }

        /// <summary>All-or-nothing: a half-bought path is never what was asked for, and the refusal has
        /// to name the wall it hit rather than collapse into a false.</summary>
        [TestMethod]
        public void ARouteThatDoesNotFitTheBudget_IsRefusedWholeAndChangesNothing()
        {
            IPassiveTreeService service = Service(points: 2);
            IReadOnlyList<string> route = service.PathTo(Third);

            Assert.AreEqual(AllocationResult.NotEnoughPoints, service.TakePath(route));
            Assert.AreEqual(0, service.SpentPoints, "a refused route bought part of itself");
            Assert.IsFalse(service.IsTaken(First));

            Assert.AreEqual(AllocationResult.NotConnected, service.TakePath([]),
                "an empty route is what a node nothing reaches prices at");
        }

        [TestMethod]
        public void TakingAndRefunding_GoThroughTheServiceAndCarryItsOwnReason()
        {
            IPassiveTreeService service = Service(points: 1);

            Assert.AreEqual(AllocationResult.NotConnected, service.Take(Third), "nothing taken touches it yet");
            Assert.AreEqual(AllocationResult.Success, service.Take(First));
            Assert.AreEqual(AllocationResult.NotEnoughPoints, service.Take(Second));
            Assert.AreEqual(AllocationResult.AlreadyTaken, service.Take(First));
            Assert.AreEqual(AllocationResult.Success, service.Refund(First));
            Assert.AreEqual(AllocationResult.NotTaken, service.Refund(First));
            Assert.AreEqual(0, service.SpentPoints);
        }

        /// <summary>The window's whole vocabulary of refusal, and the one distinction it draws that the
        /// allocation itself does not: a node nothing has reached YET is a matter of buying the way
        /// there, a node wired to nothing at all is unfinished content — and both arrive as the same
        /// verdict.</summary>
        [TestMethod]
        public void EveryRefusal_HasAKeyOfItsOwn_AndTheUnreachedIsNotTheUnwired()
        {
            PassiveTreeDocument document = Tree();

            string unreached = PassiveTreeRefusalText.TakeKey(document, Third, AllocationResult.NotConnected);
            string unwired = PassiveTreeRefusalText.TakeKey(document, Island, AllocationResult.NotConnected);

            Assert.AreNotEqual(unreached, unwired, "a node nobody has reached and a node wired to nothing read the same");

            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (AllocationResult result in Enum.GetValues<AllocationResult>())
            {
                Assert.IsTrue(keys.Add(PassiveTreeRefusalText.TakeKey(document, Third, result)), $"take {result} shares its key");
                Assert.IsTrue(keys.Add(PassiveTreeRefusalText.RefundKey(result)), $"refund {result} shares its key");
            }

            Assert.AreEqual(2 * Enum.GetValues<AllocationResult>().Length, keys.Count);
        }

        private static IPassiveTreeService Service(int points)
        {
            var service = new PassiveTreeService(new FixedTree(Tree()), ConditionCatalogs.Empty());
            service.SetTotalPoints(points);
            return service;
        }

        /// <summary>A seed with a three-node chain hanging off it, plus one node wired to nothing —
        /// the smallest tree every answer above can be asked of.</summary>
        private static PassiveTreeDocument Tree()
        {
            var document = new PassiveTreeDocument();
            document.AddNode(new PassiveNode { Id = Seed, Kind = PassiveNodeKind.Start, Stance = Stance.Strength });
            document.AddNode(new PassiveNode { Id = First, Kind = PassiveNodeKind.Small, X = 10f });
            document.AddNode(new PassiveNode { Id = Second, Kind = PassiveNodeKind.Small, X = 20f });
            document.AddNode(new PassiveNode { Id = Third, Kind = PassiveNodeKind.Notable, X = 30f });
            document.AddNode(new PassiveNode { Id = Island, Kind = PassiveNodeKind.Small, X = 400f });

            document.Link(Seed, First);
            document.Link(First, Second);
            document.Link(Second, Third);

            return document;
        }

        private sealed class FixedTree(PassiveTreeDocument tree) : IPassiveTreeProvider
        {
            public PassiveTreeDocument Tree { get; } = tree;

            public IReadOnlyList<string> Issues => [];
        }
    }
}

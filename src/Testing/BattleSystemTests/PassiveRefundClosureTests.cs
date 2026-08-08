namespace LastBreathTest.BattleSystemTests
{
    using Core.Enums;
    using Core.PassiveTree;
    using Core.PassiveTree.Allocation;

    /// <summary>
    /// What a node takes with it when it goes back. The rule that unwinding happens from the ends is not
    /// lifted anywhere — <see cref="AllocationState.CheckRefundAll"/> still refuses a set with a hole in
    /// it, and the paid gate still stands on that — so what is under test is that the CLOSURE finds the
    /// ends instead of asking the player for them, and hands them back in an order that strands nobody at
    /// any step.
    /// <para>Built on a hand-made tree. The shipped markup's ids move under an authoring pass, and a test
    /// naming one would break on a day nothing was wrong with the code.</para>
    /// </summary>
    [TestClass]
    public class PassiveRefundClosureTests
    {
        private const string Seed = "seed";
        private const string First = "first";
        private const string Second = "second";
        private const string Third = "third";
        private const string Branch = "branch";

        [TestMethod]
        public void ALeafTakesNothingWithIt()
        {
            (PassiveTreeDocument document, AllocationState allocation) = Chain();

            CollectionAssert.AreEqual(new[] { Third }, allocation.RefundClosure(document, Third).ToArray());
            CollectionAssert.AreEqual(new[] { Branch }, allocation.RefundClosure(document, Branch).ToArray());
        }

        /// <summary>The point of the whole thing: the middle of a branch names itself and everything that
        /// would be left hanging, deepest first, so removing the list in order is legal at every step.</summary>
        [TestMethod]
        public void TheMiddleOfABranchNamesItselfAndItsTail_FromTheLeavesInward()
        {
            (PassiveTreeDocument document, AllocationState allocation) = Chain();

            List<string> closure = allocation.RefundClosure(document, First);

            CollectionAssert.AreEquivalent(new[] { First, Second, Third, Branch }, closure.ToArray(),
                "something the node was holding up was left out of what it costs to give it back");

            var walk = new AllocationState();
            walk.Restore(document, allocation.Taken);
            foreach (string id in closure)
                Assert.AreEqual(AllocationResult.Success, walk.TryRefund(document, id),
                    $"removing {id} in the order named stranded something");
        }

        /// <summary>A seed comes with the character, and the whole closure has to stay out of the way of
        /// that: it answers with the seed alone so the verdict about it is the one
        /// <see cref="AllocationState.CheckRefundAll"/> gives, and an empty answer would have been read as
        /// "nothing is taken".</summary>
        [TestMethod]
        public void ASeedAnswersWithItselfAndIsRefusedAsGranted()
        {
            (PassiveTreeDocument document, AllocationState allocation) = Chain();

            List<string> closure = allocation.RefundClosure(document, Seed);

            CollectionAssert.AreEqual(new[] { Seed }, closure.ToArray());
            Assert.AreEqual(AllocationResult.Granted, allocation.CheckRefundAll(document, closure));
        }

        [TestMethod]
        public void AnUnknownOrUnheldNodeAnswersWithItself_AndKeepsItsOwnVerdict()
        {
            (PassiveTreeDocument document, AllocationState allocation) = Chain(takeChain: false);

            List<string> unknown = allocation.RefundClosure(document, "no_such_node");
            List<string> unheld = allocation.RefundClosure(document, First);

            CollectionAssert.AreEqual(new[] { "no_such_node" }, unknown.ToArray());
            CollectionAssert.AreEqual(new[] { First }, unheld.ToArray());
            Assert.AreEqual(AllocationResult.UnknownNode, allocation.CheckRefundAll(document, unknown));
            Assert.AreEqual(AllocationResult.NotTaken, allocation.CheckRefundAll(document, unheld));
        }

        /// <summary>A seed with a three-node chain hanging off it and one node branching off the first
        /// step — the shape a branch has to be given back from.</summary>
        private static (PassiveTreeDocument Document, AllocationState Allocation) Chain(bool takeChain = true)
        {
            var document = new PassiveTreeDocument();
            document.AddNode(new PassiveNode { Id = Seed, Kind = PassiveNodeKind.Start, Stance = Stance.Strength });
            Add(First, 10f);
            Add(Branch, 10f);
            Add(Second, 20f);
            Add(Third, 30f);

            document.Link(Seed, First);
            document.Link(First, Branch);
            document.Link(First, Second);
            document.Link(Second, Third);

            var allocation = new AllocationState();
            allocation.Reset(document);
            if (takeChain)
                foreach (string id in new[] { First, Second, Third, Branch })
                    Assert.IsTrue(allocation.Take(document, id, document.Nodes.Count), $"the fixture failed to take {id}");

            return (document, allocation);

            void Add(string id, float x) =>
                document.AddNode(new PassiveNode { Id = id, Kind = PassiveNodeKind.Small, X = x });
        }
    }
}

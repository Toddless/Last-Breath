namespace LastBreathTest.BattleSystemTests
{
    using Core.PassiveTree;
    using Core.Save;
    using Core.Save.Participants;
    using Moq;

    [TestClass]
    public class PassiveTreeSaveParticipantTests
    {
        [TestMethod]
        public void TreeRestoresBetweenMasteryAndItems()
        {
            // Mastery hands out the points the allocation spends; items are the other modifier source
            // on the same parameters and must settle after the tree.
            Assert.IsTrue(RestoreOrder.Mastery < RestoreOrder.PassiveTree);
            Assert.IsTrue(RestoreOrder.PassiveTree < RestoreOrder.Items);
            Assert.AreEqual(RestoreOrder.PassiveTree, ParticipantFor(new FakeTreeState(), "start_1").RestoreOrder);
        }

        [TestMethod]
        public void RoundTripsAllocatedNodesAndRemainingPoints()
        {
            var source = new FakeTreeState(availablePoints: 4, "start_1", "small_1", "notable_1");
            var captured = ParticipantFor(source, "start_1", "small_1", "notable_1").Capture();

            var target = new FakeTreeState();
            ParticipantFor(target, "start_1", "small_1", "notable_1").Restore(captured, savedVersion: 1);

            CollectionAssert.AreEquivalent(new[] { "start_1", "small_1", "notable_1" }, target.AllocatedNodes.ToArray());
            Assert.AreEqual(4, target.AvailablePoints);
        }

        [TestMethod]
        public void SaveWithoutTheSectionLoadsWithAnEmptyTree()
        {
            // Every save taken before the tree shipped looks exactly like this.
            var tree = new FakeTreeState();
            var manager = new SaveManager(new LoadScope());
            var failures = new List<string>();
            manager.Register(ParticipantFor(tree, "start_1"));
            manager.SectionRestoreFailed += (section, _) => failures.Add(section);

            manager.Restore(new SaveFile());

            Assert.AreEqual(0, tree.RestoreCalls);
            Assert.AreEqual(0, tree.AllocatedNodes.Count);
            Assert.AreEqual(0, failures.Count);
        }

        [TestMethod]
        public void NodeMissingFromTheCatalogIsSkippedAndTheRestLoads()
        {
            var source = new FakeTreeState(availablePoints: 2, "start_1", "small_deleted", "notable_1");
            var captured = ParticipantFor(source, "start_1", "small_deleted", "notable_1").Capture();

            // The catalog was rewritten under the save: small_deleted no longer exists.
            var target = new FakeTreeState();
            var manager = new SaveManager(new LoadScope());
            var failures = new List<string>();
            manager.Register(ParticipantFor(target, "start_1", "notable_1"));
            manager.SectionRestoreFailed += (section, _) => failures.Add(section);

            var file = new SaveFile();
            file.Sections["passiveTree"] = new SaveSection { Version = 1, Data = captured };
            manager.Restore(file);

            CollectionAssert.AreEquivalent(new[] { "start_1", "notable_1" }, target.AllocatedNodes.ToArray());
            Assert.AreEqual(2, target.AvailablePoints);
            Assert.AreEqual(0, failures.Count);
        }

        private static PassiveTreeSaveParticipant ParticipantFor(IPassiveTreeSaveState tree, params string[] catalogNodeIds) =>
            new(tree, CatalogWith(catalogNodeIds));

        private static IPassiveTreeProvider CatalogWith(params string[] nodeIds)
        {
            var document = new PassiveTreeDocument();
            foreach (string id in nodeIds)
                document.AddNode(new PassiveNode { Id = id });

            var provider = new Mock<IPassiveTreeProvider>();
            provider.SetupGet(p => p.Tree).Returns(document);
            return provider.Object;
        }

        private sealed class FakeTreeState : IPassiveTreeSaveState
        {
            private readonly List<string> _allocated;

            public FakeTreeState(int availablePoints = 0, params string[] allocated)
            {
                _allocated = [.. allocated];
                AvailablePoints = availablePoints;
            }

            public IReadOnlyCollection<string> AllocatedNodes => _allocated;

            public int AvailablePoints { get; private set; }

            public int RestoreCalls { get; private set; }

            public void RestoreState(IReadOnlyCollection<string> allocatedNodes, int availablePoints)
            {
                RestoreCalls++;
                _allocated.Clear();
                _allocated.AddRange(allocatedNodes);
                AvailablePoints = availablePoints;
            }
        }
    }
}

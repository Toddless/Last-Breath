namespace LastBreathTest.BattleSystemTests
{
    using Core.Ai.World.Time;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Data.SaveData;
    using Core.Entity;
    using Core.Enums;
    using Core.Items.Grants;
    using Core.PassiveTree;
    using Core.PassiveTree.Allocation;
    using Core.Save;
    using Core.Save.Participants;
    using Core.Services;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// The save stores the taken set and only that. Points are not part of it: mastery owns the
    /// granted total and restores first, so a stored remainder could only contradict it. What the
    /// file carries is not trusted either — the service re-checks it against the current tree, which
    /// is what keeps a save taken against yesterday's content from producing an allocation the
    /// take/refund rules could never have produced.
    /// </summary>
    [TestClass]
    public class PassiveTreeSaveParticipantTests
    {
        private const string Seed = "start_1";
        private const string First = "small_1";
        private const string Second = "small_2";
        private const string Third = "small_3";

        [TestMethod]
        public void TreeRestoresBetweenMasteryAndItems()
        {
            // Mastery hands out the points the allocation spends; items are the other modifier source
            // on the same parameters and must settle after the tree.
            Assert.IsTrue(RestoreOrder.Mastery < RestoreOrder.PassiveTree);
            Assert.IsTrue(RestoreOrder.PassiveTree < RestoreOrder.Items);
            Assert.AreEqual(RestoreOrder.PassiveTree, new PassiveTreeSaveParticipant(ServiceOn(Chain(First), points: 1)).RestoreOrder);
        }

        [TestMethod]
        public void RoundTripsTheTakenSet()
        {
            IPassiveTreeService source = ServiceOn(Chain(First, Second), points: 5);
            source.Take(First);
            source.Take(Second);
            JToken captured = new PassiveTreeSaveParticipant(source).Capture();

            IPassiveTreeService target = ServiceOn(Chain(First, Second), points: 5);
            new PassiveTreeSaveParticipant(target).Restore(captured, savedVersion: 1);

            CollectionAssert.AreEquivalent(new[] { Seed, First, Second }, target.TakenNodes.ToArray());
            Assert.AreEqual(2, target.SpentPoints);
            Assert.AreEqual(3, target.AvailablePoints);
        }

        [TestMethod]
        public void ThePointsAreNotStored_TheRemainderFollowsTheGrantedTotal()
        {
            IPassiveTreeService source = ServiceOn(Chain(First), points: 5);
            source.Take(First);
            JToken captured = new PassiveTreeSaveParticipant(source).Capture();

            Assert.IsNull(captured["availablePoints"],
                "the remainder must not travel in the save — restoring it would overwrite the total mastery grants");

            // Mastery granted one more point since the save was taken (a level, or a rebalanced curve).
            IPassiveTreeService target = ServiceOn(Chain(First), points: 6);
            new PassiveTreeSaveParticipant(target).Restore(captured, savedVersion: 1);

            Assert.AreEqual(6, target.TotalPoints, "the restore reached into the granted total");
            Assert.AreEqual(5, target.AvailablePoints, "the remainder must follow the new total, not the saved one");
        }

        [TestMethod]
        public void SaveWithoutTheSectionLoadsWithAnEmptyTree()
        {
            // Every save taken before the tree shipped looks exactly like this.
            IPassiveTreeService tree = ServiceOn(Chain(First), points: 5);
            var manager = new SaveManager(new LoadScope());
            var failures = new List<string>();
            manager.Register(new PassiveTreeSaveParticipant(tree));
            manager.SectionRestoreFailed += (section, _) => failures.Add(section);

            manager.Restore(new SaveFile());

            CollectionAssert.AreEquivalent(new[] { Seed }, tree.TakenNodes.ToArray(), "nothing but the granted seed may be taken");
            Assert.AreEqual(0, tree.SpentPoints);
            Assert.AreEqual(0, failures.Count);
        }

        [TestMethod]
        public void NodeMissingFromTheCatalogIsSkippedAndTheRestLoads()
        {
            IPassiveTreeService source = ServiceOn(Chain(First, Second), points: 5);
            source.Take(First);
            source.Take(Second);
            JToken captured = new PassiveTreeSaveParticipant(source).Capture();

            // The catalog was rewritten under the save: the second node no longer exists.
            IPassiveTreeService target = ServiceOn(Chain(First), points: 5);
            var manager = new SaveManager(new LoadScope());
            var failures = new List<string>();
            manager.Register(new PassiveTreeSaveParticipant(target));
            manager.SectionRestoreFailed += (section, _) => failures.Add(section);

            manager.Restore(FileWith(captured));

            CollectionAssert.AreEquivalent(new[] { Seed, First }, target.TakenNodes.ToArray());
            Assert.AreEqual(1, target.SpentPoints);
            Assert.AreEqual(0, failures.Count, "a node that left the catalog is content drift, not a broken save");
        }

        [TestMethod]
        public void AnAllocationThatLostItsRouteToASeed_ArrivesTrimmed()
        {
            // Every id still exists, so the participant's own filter passes the set through whole; the
            // link the middle node used to provide is gone, and the tail hangs in the air.
            var saved = JToken.FromObject(new PassiveTreeSaveData { Allocated = [Seed, First, Third] });
            IPassiveTreeService target = ServiceOn(Chain(First, Second, Third), points: 5);

            new PassiveTreeSaveParticipant(target).Restore(saved, savedVersion: 1);

            CollectionAssert.AreEquivalent(new[] { Seed, First }, target.TakenNodes.ToArray(),
                "the stranded node must not come back — the take rules could never have produced it");
            Assert.AreEqual(1, target.SpentPoints, "the stranded node must not stay paid for either");
        }

        [TestMethod]
        public void TheSectionIsWrittenAndReadThroughTheRealRegistration()
        {
            // No stand-in for the wiring: the section has to survive the container the game builds.
            IPassiveTreeService source = ServiceOn(Chain(First, Second), points: 5);
            source.Take(First);

            SaveFile file = ManagerFor(source).Capture(new SaveMetadata());

            Assert.IsTrue(file.Sections.ContainsKey("passiveTree"), "the registered save stack did not write the tree section");

            IPassiveTreeService target = ServiceOn(Chain(First, Second), points: 5);
            ISaveManager into = ManagerFor(target);
            var failures = new List<string>();
            into.SectionRestoreFailed += (section, _) => failures.Add(section);

            into.Restore(file);

            CollectionAssert.AreEquivalent(new[] { Seed, First }, target.TakenNodes.ToArray());
            Assert.AreEqual(0, failures.Count, string.Join("; ", failures));
        }

        [TestMethod]
        public void AProjectWithoutTheTreeServiceWritesNoSection()
        {
            // Battle and the sandboxes register the save stack without the tree.
            SaveFile file = ManagerFor(tree: null).Capture(new SaveMetadata());

            Assert.IsFalse(file.Sections.ContainsKey("passiveTree"));
        }

        private static SaveFile FileWith(JToken section)
        {
            var file = new SaveFile();
            file.Sections["passiveTree"] = new SaveSection { Version = 1, Data = section };
            return file;
        }

        /// <summary>The save stack exactly as a project builds it, with the tree service in place.</summary>
        private static ISaveManager ManagerFor(IPassiveTreeService? tree)
        {
            var services = new ServiceCollection();
            if (tree != null) services.AddSingleton(tree);

            services.AddSingleton(Mock.Of<IWorldClock>());
            services.AddSingleton(Mock.Of<IFactionRelationService>());
            services.AddSingleton(Mock.Of<IMartialArtMastery>());
            services.AddSingleton(Mock.Of<IPlayerAccessor>());
            services.AddSingleton(Mock.Of<IAbilityProvider>());
            services.AddSingleton(Mock.Of<IGrantFactory>());
            services.AddSingleton(Mock.Of<ISpawnPointRegistry>(registry => registry.All == new List<IPersistentSpawnPoint>()));
            services.AddSaveSystem();

            return services.BuildServiceProvider().GetRequiredService<ISaveManager>();
        }

        private static IPassiveTreeService ServiceOn(PassiveTreeDocument document, int points)
        {
            var service = new PassiveTreeService(new TreeProviderStub(document));
            service.SetTotalPoints(points);
            return service;
        }

        /// <summary>A seed with the given Small nodes chained off it, each reachable only through the
        /// previous one — the smallest shape where connectivity can actually be broken.</summary>
        private static PassiveTreeDocument Chain(params string[] ids)
        {
            var document = new PassiveTreeDocument();
            document.AddNode(new PassiveNode { Id = Seed, Kind = PassiveNodeKind.Start, Stance = Stance.Strength });

            string previous = Seed;
            foreach (string id in ids)
            {
                document.AddNode(new PassiveNode { Id = id, Kind = PassiveNodeKind.Small, Stance = Stance.Strength });
                document.Link(previous, id);
                previous = id;
            }

            return document;
        }

        private sealed class TreeProviderStub(PassiveTreeDocument tree) : IPassiveTreeProvider
        {
            public PassiveTreeDocument Tree { get; } = tree;

            public IReadOnlyList<string> Issues => [];
        }
    }
}

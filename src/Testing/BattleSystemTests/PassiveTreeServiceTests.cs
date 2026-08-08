namespace LastBreathTest.BattleSystemTests
{
    using Core.Data.GameData;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Modifiers;
    using Core.Modifiers.Conditions;
    using Core.PassiveTree;
    using Core.PassiveTree.Allocation;
    using Core.Save;
    using Core.Session;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// The tree reaches a fighter through a single channel — a registered modifier source — and never
    /// through the entity's own modifier list. That is what makes a respec exact: the contribution is
    /// recomputed from the taken set instead of being hunted down and deleted, so no tree line can be
    /// left behind and no foreign line can be swept away with it.
    /// </summary>
    [TestClass]
    public class PassiveTreeServiceTests
    {
        private const float BaseStrength = 10f;

        /// <summary>Flat Strength carried by the one gated line the fixtures below hang on a node.</summary>
        private const float ConditionalGrant = 4f;

        private const float CarrierHealth = 100f;

        [TestMethod]
        public void TakingANode_RaisesTheParameter_AndRefundingReturnsItExactly()
        {
            var fighter = new Fighter();
            IPassiveTreeService service = ShippedService(points: 5);
            NodeChain chain = Chain(service);
            fighter.Modifiers.RegisterSource(service.ParameterSource);
            fighter.Parameters.SetBaseValueForParameter(EntityParameter.Strength, BaseStrength);

            Assert.AreEqual(AllocationResult.Success, service.Take(chain.First));
            Assert.AreEqual(BaseStrength + chain.FirstStrength, fighter[EntityParameter.Strength], 0f);

            Assert.AreEqual(AllocationResult.Success, service.Refund(chain.First));
            Assert.AreEqual(BaseStrength, fighter[EntityParameter.Strength], 0f, "the refund must land exactly on the base value");
            Assert.AreEqual(0, service.SpentPoints);
        }

        [TestMethod]
        public void RefundingANode_LeavesTheContributionOfItemsOnTheSameParameterUntouched()
        {
            var fighter = new Fighter();
            IPassiveTreeService service = ShippedService(points: 5);
            NodeChain chain = Chain(service);
            var worn = new StubSource(new SimpleModifier(EntityParameter.Strength, ModifierValueType.Flat, 5f, "TestItem"));
            var loose = new SimpleModifier(EntityParameter.Strength, ModifierValueType.Flat, 3f, "TestBuff");

            fighter.Modifiers.RegisterSource(service.ParameterSource);
            fighter.Modifiers.RegisterSource(worn);
            fighter.Modifiers.AddModifier(loose);
            fighter.Parameters.SetBaseValueForParameter(EntityParameter.Strength, BaseStrength);
            float withoutTree = fighter[EntityParameter.Strength];

            service.Take(chain.First);
            Assert.AreEqual(withoutTree + chain.FirstStrength, fighter[EntityParameter.Strength], 0f);

            service.Refund(chain.First);

            Assert.AreEqual(withoutTree, fighter[EntityParameter.Strength], 0f, "the refund took more than the node gave");
            Assert.AreEqual(BaseStrength + 5f + 3f, withoutTree, 0f, "the fixture itself lost a contribution");
            CollectionAssert.Contains(fighter.Modifiers.EntityModifiers[EntityParameter.Strength], loose,
                "a tree refund must not touch modifiers the entity owns");
            Assert.IsTrue(fighter.Modifiers.GetModifiers(EntityParameter.Strength).Any(modifier => modifier.Source == "TestItem"),
                "a tree refund must not touch another source");
        }

        [TestMethod]
        public void TreeLines_NeverEnterTheEntityModifierList()
        {
            var fighter = new Fighter();
            IPassiveTreeService service = ShippedService(points: 5);
            fighter.Modifiers.RegisterSource(service.ParameterSource);

            service.Take(Chain(service).First);

            AssertNothingOfTheTreeIsOwnedBy(fighter.Modifiers);
            Assert.AreEqual(1, TreeLinesFor(fighter, EntityParameter.Strength),
                "one taken node with one Strength line must resolve to exactly one modifier");

            // The conditional line is the one with a second way in: its predicate has to be pointed at the
            // fighter, and the call that does that sits beside the one that also writes the modifier into
            // his list. Taken together the line lands twice — once where he pulls it from, once where he
            // keeps his own — and the copy he keeps is the one a respec cannot reach.
            ConditionOwner carrier = WoundedCarrier(out IPassiveTreeService gated);

            Assert.AreEqual(AllocationResult.Success, gated.Take(SyntheticNode));

            AssertNothingOfTheTreeIsOwnedBy(carrier.ParameterModifiers);
            Assert.AreEqual(BaseStrength + ConditionalGrant, carrier.Parameters.GetValueForParameter(EntityParameter.Strength), 0f,
                "the armed line reached the character by a second route as well as through the source");
        }

        [TestMethod]
        public void AnAggregateLine_ReachesEveryFamilyMemberExactlyOnce()
        {
            const float grant = 5f;
            var fighter = new Fighter();
            IPassiveTreeService service = SyntheticService(points: 1,
                new ModifierLine { Parameter = EntityParameter.AllAttribute, ValueType = ModifierValueType.Flat, Value = grant });
            fighter.Modifiers.RegisterSource(service.ParameterSource);

            foreach (EntityParameter attribute in Attributes)
                fighter.Parameters.SetBaseValueForParameter(attribute, BaseStrength);

            Assert.AreEqual(AllocationResult.Success, service.Take(SyntheticNode));

            foreach (EntityParameter attribute in Attributes)
            {
                Assert.AreEqual(BaseStrength + grant, fighter[attribute], 0f, $"{attribute} missed the aggregate line");
                Assert.AreEqual(1, TreeLinesFor(fighter, attribute), $"{attribute} folded the aggregate line more than once");
            }

            service.Refund(SyntheticNode);

            foreach (EntityParameter attribute in Attributes)
                Assert.AreEqual(BaseStrength, fighter[attribute], 0f, $"{attribute} kept part of the aggregate line");
        }

        [TestMethod]
        public void TakingANodeNothingLinksTo_IsRefusedInsteadOfThrowing()
        {
            // The document is built here rather than taken from the markup: a node with no edge is a
            // defect the author has since cleaned out (the case below holds him to it), and the rule
            // still has to answer for the day an edit leaves one behind.
            PassiveTreeDocument document = SeedOnlyTree();
            document.AddNode(new PassiveNode { Id = UnlinkedNode, Kind = PassiveNodeKind.Small, Stance = Stance.Strength });
            IPassiveTreeService service = Service(document, points: 5);

            Assert.AreEqual(0, service.Tree.Neighbours(UnlinkedNode).Count, "the fixture linked the node the case is about");

            Assert.AreEqual(AllocationResult.NotConnected, service.Take(UnlinkedNode));
            Assert.IsFalse(service.IsTaken(UnlinkedNode));
            Assert.AreEqual(0, service.SpentPoints, "a refused purchase was charged for");
        }

        [TestMethod]
        public void TakingAnIdThatIsNotInTheTree_IsRefusedAsUnknown()
        {
            IPassiveTreeService service = ShippedService(points: 5);

            Assert.AreEqual(AllocationResult.UnknownNode, service.Take("no_such_node"));
            Assert.AreEqual(AllocationResult.UnknownNode, service.Refund("no_such_node"));
        }

        [TestMethod]
        public void RefundingFromTheMiddleOfAPath_IsRefused_AndFromItsEndPasses()
        {
            IPassiveTreeService service = ShippedService(points: 5);
            NodeChain chain = Chain(service);
            service.Take(chain.First);
            service.Take(chain.Second);
            service.Take(chain.Third);

            Assert.AreEqual(AllocationResult.WouldOrphan, service.Refund(chain.Second), "refunding the middle strands the end");
            Assert.AreEqual(AllocationResult.Success, service.Refund(chain.Third));
            Assert.AreEqual(AllocationResult.Success, service.Refund(chain.Second), "the middle becomes an end once the tail is gone");
            Assert.AreEqual(1, service.SpentPoints);
        }

        [TestMethod]
        public void GrantedPoints_CapWhatCanBeTaken()
        {
            IPassiveTreeService service = ShippedService(points: 1);
            NodeChain chain = Chain(service);

            Assert.AreEqual(AllocationResult.Success, service.Take(chain.First));
            Assert.AreEqual(0, service.AvailablePoints);
            Assert.AreEqual(AllocationResult.NotEnoughPoints, service.Take(chain.Second));

            service.SetTotalPoints(2);

            Assert.AreEqual(AllocationResult.Success, service.Take(chain.Second));
            Assert.AreEqual(2, service.SpentPoints);
            Assert.AreEqual(0, service.AvailablePoints);
        }

        [TestMethod]
        public void Seeds_AreGrantedFreeAndCannotBeGivenBack()
        {
            IPassiveTreeService service = ShippedService(points: 5);
            string[] seeds = service.Tree.Nodes.Where(node => node.Kind == PassiveNodeKind.Start).Select(node => node.Id).ToArray();

            Assert.IsTrue(seeds.Length > 0, "the shipped tree has no seed to start from");
            Assert.AreEqual(0, service.SpentPoints, "seeds must not cost a point");

            foreach (string seed in seeds)
            {
                Assert.IsTrue(service.IsTaken(seed), $"{seed} is granted with the character");
                Assert.AreEqual(AllocationResult.AlreadyTaken, service.Take(seed));
                Assert.AreEqual(AllocationResult.Granted, service.Refund(seed));
            }
        }

        [TestMethod]
        public void Respec_ReturnsEveryParameterItTouchedToItsBase()
        {
            var fighter = new Fighter();
            IPassiveTreeService service = ShippedService(points: 5);
            NodeChain chain = Chain(service);
            fighter.Modifiers.RegisterSource(service.ParameterSource);

            foreach (EntityParameter parameter in chain.Parameters)
                fighter.Parameters.SetBaseValueForParameter(parameter, BaseStrength);

            float[] withoutTree = [.. chain.Parameters.Select(parameter => fighter[parameter])];

            Assert.AreEqual(AllocationResult.Success, service.Take(chain.First));
            Assert.AreEqual(AllocationResult.Success, service.Take(chain.Second));
            Assert.AreEqual(AllocationResult.Success, service.Take(chain.Third));
            Assert.AreEqual(3, service.SpentPoints, "the fixture never bought the chain the respec is about");

            foreach (EntityParameter parameter in chain.Parameters)
                Assert.IsTrue(TreeLinesFor(fighter, parameter) > 0, $"{parameter} was never touched — the respec has nothing to return");

            service.Respec();

            Assert.AreEqual(0, service.SpentPoints);

            for (int index = 0; index < chain.Parameters.Count; index++)
            {
                EntityParameter parameter = chain.Parameters[index];
                Assert.AreEqual(withoutTree[index], fighter[parameter], 0f, $"{parameter} did not land back on its base");
                Assert.AreEqual(0, TreeLinesFor(fighter, parameter), $"{parameter} kept a line of the tree");
            }
        }

        [TestMethod]
        public void TheTakenSetAnnouncesEveryChange()
        {
            IPassiveTreeService service = ShippedService(points: 5);
            NodeChain chain = Chain(service);
            Assert.IsTrue(service.TakenNodes.Count > 0, "adopting the loaded document puts the granted seeds in the set");

            int changes = 0;
            service.AllocationChanged += () => changes++;

            service.Take(chain.First);
            service.Take("no_such_node");
            service.Refund(chain.First);
            service.Respec();

            Assert.AreEqual(3, changes, "a refused request must not announce a change");
        }

        [TestMethod]
        public void EveryAccessorAdoptsTheDocument_NotOnlyTheTreeGetter()
        {
            // The save participant and the modifier wiring never ask for Tree. An accessor that
            // answered before the document was adopted would report an empty allocation — and a
            // capture would write an empty section over a real one.
            var provider = new TreeProviderStub(SyntheticTree());
            var service = new PassiveTreeService(provider, ConditionCatalogs.Empty());
            service.SetTotalPoints(1);

            Assert.IsTrue(service.TakenNodes.Contains(SyntheticSeed), "TakenNodes answered before the document was adopted");
            Assert.AreEqual(AllocationResult.Success, service.Take(SyntheticNode));
            Assert.AreEqual(1, service.SpentPoints);

            // The catalog was reloaded underneath and the bought node went with it.
            provider.Tree = SeedOnlyTree();

            Assert.IsFalse(service.IsTaken(SyntheticNode), "IsTaken answered from the document that was replaced");
            Assert.AreEqual(0, service.SpentPoints, "SpentPoints answered from the document that was replaced");
        }

        [TestMethod]
        public void CheckTake_GivesTheAnswerWithoutBuying()
        {
            IPassiveTreeService service = ShippedService(points: 1);
            NodeChain chain = Chain(service);

            Assert.AreEqual(AllocationResult.Success, service.CheckTake(chain.First));
            Assert.AreEqual(0, service.SpentPoints, "a check must not spend a point");
            Assert.IsFalse(service.IsTaken(chain.First), "a check must not take the node");
            Assert.AreEqual(AllocationResult.UnknownNode, service.CheckTake("no_such_node"));

            service.Take(chain.First);

            Assert.AreEqual(AllocationResult.AlreadyTaken, service.CheckTake(chain.First));
            Assert.AreEqual(AllocationResult.NotEnoughPoints, service.CheckTake(chain.Second));
        }

        [TestMethod]
        public void TheSourceHandsOutSnapshots_NotTheCollectionsItRebuilds()
        {
            // RegisterSource walks AffectedParameters while announcing the source; a handler that ends
            // in another Rebuild would empty the live key set out from under that walk.
            IPassiveTreeService service = ShippedService(points: 5);
            service.Take(Chain(service).First);
            IReadOnlyCollection<EntityParameter> affected = service.ParameterSource.AffectedParameters;
            IEnumerable<IModifierInstance> lines = service.ParameterSource.GetModifiers(EntityParameter.Strength);

            service.Respec();

            Assert.IsTrue(affected.Count > 0, "the live key set was handed out — the rebuild emptied it mid-walk");
            Assert.AreEqual(1, lines.Count(), "the line list was handed out live — the rebuild changed it after the fact");
            Assert.IsFalse(lines is ICollection<IModifierInstance> { IsReadOnly: false },
                "the source handed out the list it edits — a caller could rewrite the tree's contribution in place");
        }

        [TestMethod]
        public void ANewSession_DropsTheAllocationAndTheTotalThatPaidForIt()
        {
            // The service is a singleton and outlives the scene: a second "New game" in one process
            // would otherwise hand the fresh character the previous playthrough's nodes — with the
            // mastery that paid for them already back at zero, so nothing could be bought either.
            var fighter = new Fighter();
            IPassiveTreeService service = ShippedService(points: 5);
            NodeChain chain = Chain(service);
            fighter.Modifiers.RegisterSource(service.ParameterSource);
            fighter.Parameters.SetBaseValueForParameter(EntityParameter.Strength, BaseStrength);
            service.Take(chain.First);
            service.Take(chain.Second);

            int changes = 0;
            service.AllocationChanged += () => changes++;

            ((ISessionResettable)service).ResetSession();

            Assert.AreEqual(0, service.TotalPoints, "the granted total survived the new game");
            Assert.AreEqual(0, service.SpentPoints);
            CollectionAssert.AreEquivalent(SeedsOf(service), service.TakenNodes.ToArray(), "only the free seeds start a character");
            Assert.AreEqual(BaseStrength, fighter[EntityParameter.Strength], 0f, "the fresh character wears the old playthrough's stats");
            Assert.AreEqual(0, TreeLinesFor(fighter, EntityParameter.Strength));
            Assert.AreEqual(1, changes, "the reset must announce itself — the source is rebuilt behind it");
        }

        [TestMethod]
        public void TheSessionResetStack_ReachesTheTree()
        {
            // No stand-in for the wiring: the reset has to reach the tree through the container the
            // game builds, not only through a hand-made cast.
            IPassiveTreeService service = ShippedService(points: 5);
            service.Take(Chain(service).First);

            var services = new ServiceCollection();
            services.AddSingleton<LoadScope>();
            services.AddSingleton(service);
            services.AddSessionReset();

            services.BuildServiceProvider().GetRequiredService<ISessionResetService>().ResetSession();

            Assert.AreEqual(0, service.SpentPoints, "the registered reset stack does not know about the tree");
            Assert.AreEqual(0, service.TotalPoints);
        }

        [TestMethod]
        public void ADocumentThatFailedToParse_DoesNotEatTheAllocation()
        {
            const float nodeStrength = 4f;
            var fighter = new Fighter();
            var provider = new TreeProviderStub(SyntheticTree(new ModifierLine
            {
                Parameter = EntityParameter.Strength, ValueType = ModifierValueType.Flat, Value = nodeStrength
            }));
            var service = new PassiveTreeService(provider, ConditionCatalogs.Empty());
            fighter.Modifiers.RegisterSource(service.ParameterSource);
            fighter.Parameters.SetBaseValueForParameter(EntityParameter.Strength, BaseStrength);
            service.SetTotalPoints(1);

            Assert.AreEqual(AllocationResult.Success, service.Take(SyntheticNode));

            // Exactly what the reader hands out when the file does not parse: a document with nothing
            // in it. Read as content it means "every node was deleted"; it means the file is broken.
            List<string> issues = [];
            provider.Tree = PassiveTreeSerializer.Deserialize("{ \"nodes\": [", issues);

            Assert.AreEqual(1, issues.Count, "the fixture must actually be a file that failed to parse");
            Assert.IsTrue(service.IsTaken(SyntheticNode), "a broken file took the allocation with it");
            Assert.AreEqual(1, service.SpentPoints, "a broken file also refunded what the allocation cost");
            Assert.AreEqual(BaseStrength + nodeStrength, fighter[EntityParameter.Strength], 0f, "the node's contribution was dropped");
            Assert.IsNotNull(service.Tree.Find(SyntheticNode), "the last document that did load must stay in force");
        }

        /// <summary>
        /// Three nodes of the shipped tree in a row: the first hangs off a granted seed and is the whole
        /// of what its carrier gets on Strength, and each of the other two is reachable only through the
        /// one before it. That shape is everything the cases below need — naming ids instead would tie
        /// them to a corner of a wheel the author redraws whenever he pleases.
        /// <para><paramref name="Parameters"/> is what the three of them actually reach, read off the
        /// nodes themselves. A case about giving a contribution back has to name the parameters the
        /// contribution is made of: naming them by hand would hold whether the chain touched them or not,
        /// and go quiet the day the author redraws the run onto other stats.</para>
        /// </summary>
        private sealed record NodeChain(string First, string Second, string Third, float FirstStrength,
            IReadOnlyList<EntityParameter> Parameters);

        /// <summary>The chain found in the very document the service serves, so the fixture and the
        /// subject can never be two different trees.</summary>
        private static NodeChain Chain(IPassiveTreeService service)
        {
            NodeChain? chain = FindChain(service.Tree);

            Assert.IsNotNull(chain,
                "the shipped tree holds no seed—node—node—node run whose head grants flat Strength: every case about paths needs one");

            return chain;
        }

        private static NodeChain? FindChain(PassiveTreeDocument tree)
        {
            HashSet<string> granted = [.. tree.Nodes.Where(node => !NodeKindRules.CostsPoint(node.Kind)).Select(node => node.Id)];

            foreach (PassiveNode first in tree.Nodes)
            {
                if (granted.Contains(first.Id) || !TouchesGranted(tree, granted, first.Id)) continue;
                if (OnlyFlatStrength(first) is not float strength) continue;

                foreach (string second in tree.Neighbours(first.Id))
                {
                    // The middle may not have a road of its own back to a seed, or refunding it would
                    // strand nobody and the case about orphans would pass on a chain that is not one.
                    if (granted.Contains(second) || TouchesGranted(tree, granted, second)) continue;

                    foreach (string third in tree.Neighbours(second))
                        if (third != first.Id && !granted.Contains(third)
                            && !TouchesGranted(tree, granted, third) && !tree.AreLinked(first.Id, third))
                            return new NodeChain(first.Id, second, third, strength, ParametersOf(tree, first.Id, second, third));
                }
            }

            return null;
        }

        /// <summary>Every parameter a taken run of nodes puts a line on, aggregates already folded into
        /// the family they stand for. Conditional and flag lines are left out: neither reaches a
        /// parameter through a fixture with no catalog behind it, so counting them would promise a
        /// contribution the entity never sees.</summary>
        private static EntityParameter[] ParametersOf(PassiveTreeDocument tree, params string[] ids) =>
            [.. ids.Select(tree.Find)
                .SelectMany(node => node?.Modifiers ?? [])
                .Where(line => !line.IsConditional && line.ValueType != ModifierValueType.Flag)
                .SelectMany(line => Reached(line.Parameter))
                .Distinct()];

        /// <summary>The concrete parameters a line lands on — the aggregate's family where it names one,
        /// and the parameter itself otherwise.</summary>
        private static IReadOnlyList<EntityParameter> Reached(EntityParameter parameter) =>
            AggregateParameters.Members(parameter) is { Count: > 0 } members ? members : [parameter];

        private static bool TouchesGranted(PassiveTreeDocument tree, HashSet<string> granted, string id) =>
            tree.Neighbours(id).Any(granted.Contains);

        /// <summary>The flat Strength a node hands out, when one unconditional flat line is the whole of
        /// what it gives that parameter. The cases that count Strength need to know everything reaching
        /// it — including a line written as the aggregate the family folds back in.</summary>
        private static float? OnlyFlatStrength(PassiveNode node)
        {
            List<ModifierLine> reaching = [.. node.Modifiers.Where(line => Reaches(line.Parameter, EntityParameter.Strength))];

            return reaching is [{ ValueType: ModifierValueType.Flat, IsConditional: false } line] ? line.Value : null;
        }

        private static bool Reaches(EntityParameter line, EntityParameter parameter) =>
            line == parameter || AggregateParameters.Members(line).Contains(parameter);

        private static string[] SeedsOf(IPassiveTreeService service) =>
            service.Tree.Nodes.Where(node => node.Kind == PassiveNodeKind.Start).Select(node => node.Id).ToArray();

        private static EntityParameter[] Attributes =>
            [EntityParameter.Strength, EntityParameter.Dexterity, EntityParameter.Intelligence];

        private const string SyntheticNode = "small_1";

        private const string SyntheticSeed = "start";

        /// <summary>A node put in the document with no edge at all — the defect the shipped markup no longer has.</summary>
        private const string UnlinkedNode = "small_unlinked";

        private static int TreeLinesFor(Fighter fighter, EntityParameter parameter) =>
            fighter.Modifiers.GetModifiers(parameter).Count(modifier => modifier.Source == PassiveTreeDocument.ModifierSource);

        private static void AssertNothingOfTheTreeIsOwnedBy(IParameterModifiersComponent modifiers) =>
            Assert.IsFalse(modifiers.EntityModifiers.Values.SelectMany(lines => lines)
                    .Any(modifier => modifier.Source == PassiveTreeDocument.ModifierSource),
                "the tree wrote into the entity's own modifier list — that is the double-count channel");

        /// <summary>A fighter whose state already answers the predicate, wired to a tree whose one
        /// purchasable node carries a line gated on it. Wired the way a player is: the parametric half is
        /// registered, the context half is handed over, and it is the hand-over that tells the predicates
        /// whom to read.</summary>
        private static ConditionOwner WoundedCarrier(out IPassiveTreeService service)
        {
            service = Service(SyntheticTree(new ModifierLine
            {
                Parameter = EntityParameter.Strength,
                ValueType = ModifierValueType.Flat,
                Value = ConditionalGrant,
                Condition = ConditionCatalogs.WhileWounded
            }), points: 1, ConditionCatalogs.Wounded());

            var carrier = new ConditionOwner();
            carrier.SetMaximum(EntityParameter.Health, CarrierHealth);
            carrier.CurrentHealth = CarrierHealth * (ConditionCatalogs.WoundedShare - 0.1f);
            carrier.Parameters.SetBaseValueForParameter(EntityParameter.Strength, BaseStrength);
            carrier.ParameterModifiers.RegisterSource(service.ParameterSource);
            service.ContextSource.Attach(carrier);

            return carrier;
        }

        private static IPassiveTreeService ShippedService(int points) => Service(ShippedTree(), points);

        private static IPassiveTreeService SyntheticService(int points, params ModifierLine[] lines) =>
            Service(SyntheticTree(lines), points);

        /// <summary>A seed with one Small node hanging off it — the smallest tree a purchase can happen on.</summary>
        private static PassiveTreeDocument SyntheticTree(params ModifierLine[] lines)
        {
            PassiveTreeDocument document = SeedOnlyTree();
            var node = new PassiveNode { Id = SyntheticNode, Kind = PassiveNodeKind.Small, Stance = Stance.Strength };
            foreach (ModifierLine line in lines) node.Modifiers.Add(line);
            document.AddNode(node);
            document.Link(SyntheticSeed, SyntheticNode);

            return document;
        }

        /// <summary>The same tree after its only purchasable node was deleted from the catalog.</summary>
        private static PassiveTreeDocument SeedOnlyTree()
        {
            var document = new PassiveTreeDocument();
            document.AddNode(new PassiveNode { Id = SyntheticSeed, Kind = PassiveNodeKind.Start, Stance = Stance.Strength });
            return document;
        }

        private static IPassiveTreeService Service(PassiveTreeDocument document, int points) =>
            Service(document, points, ConditionCatalogs.Empty());

        private static IPassiveTreeService Service(PassiveTreeDocument document, int points, ConditionProvider conditions)
        {
            var service = new PassiveTreeService(new TreeProviderStub(document), conditions);
            service.SetTotalPoints(points);
            return service;
        }

        private static PassiveTreeDocument ShippedTree()
        {
            List<string> issues = [];
            PassiveTreeDocument document = PassiveTreeSerializer.Deserialize(File.ReadAllText(ShippedTreePath()), issues);
            Assert.AreEqual(0, issues.Count, string.Join("; ", issues));
            return document;
        }

        private static string ShippedTreePath()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null)
            {
                string candidate = Path.Combine(directory.FullName, "Data", "Shared", DataCatalog.PassiveTree, PassiveTreeFormat.DefaultFileName);
                if (File.Exists(candidate)) return candidate;
                directory = directory.Parent;
            }

            throw new InvalidOperationException($"{DataCatalog.PassiveTree} catalog not found above {AppContext.BaseDirectory}");
        }

        /// <summary>The modifier/parameter pair wired the way a fighter wires it.</summary>
        private sealed class Fighter
        {
            public ParameterModifiersComponent Modifiers { get; } = new();

            public EntityParametersComponent Parameters { get; } = new();

            public Fighter()
            {
                Parameters.Initialize(Modifiers.GetModifiers);
                Modifiers.ModifiersChanged += Parameters.OnParameterModifiersChange;
            }

            public float this[EntityParameter parameter] => Parameters.GetValueForParameter(parameter);
        }

        /// <summary>Stands in for equipment: another registered source contributing to the same parameter.</summary>
        private sealed class StubSource(params IModifierInstance[] modifiers) : IParameterModifierSource
        {
            public event Action<IReadOnlyCollection<EntityParameter>>? SourceChanged;

            public IReadOnlyCollection<EntityParameter> AffectedParameters =>
                modifiers.Select(modifier => modifier.EntityParameter).ToHashSet();

            public IEnumerable<IModifierInstance> GetModifiers(EntityParameter parameter) =>
                modifiers.Where(modifier => modifier.EntityParameter == parameter);
        }

        /// <summary>Stands in for the data pipeline, including a reload: the document can be swapped
        /// the way a catalog reload swaps it.</summary>
        private sealed class TreeProviderStub(PassiveTreeDocument tree) : IPassiveTreeProvider
        {
            public PassiveTreeDocument Tree { get; set; } = tree;

            public IReadOnlyList<string> Issues => [];
        }
    }
}

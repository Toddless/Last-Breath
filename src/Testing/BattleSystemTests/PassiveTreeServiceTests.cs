namespace LastBreathTest.BattleSystemTests
{
    using Core.Data.GameData;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Modifiers;
    using Core.PassiveTree;
    using Core.PassiveTree.Allocation;

    /// <summary>
    /// The tree reaches a fighter through a single channel — a registered modifier source — and never
    /// through the entity's own modifier list. That is what makes a respec exact: the contribution is
    /// recomputed from the taken set instead of being hunted down and deleted, so no tree line can be
    /// left behind and no foreign line can be swept away with it.
    /// </summary>
    [TestClass]
    public class PassiveTreeServiceTests
    {
        /// <summary>A chain hanging off the seed: the third node is only reachable through the second.</summary>
        private const string FirstNode = "small_dex_strength_1";
        private const string SecondNode = "small_dex_strength_2";
        private const string ThirdNode = "small_dex_strength_3";

        /// <summary>Flat Strength carried by every node of the chain above.</summary>
        private const float ChainStrengthPerNode = 2f;

        private const float BaseStrength = 10f;

        [TestMethod]
        public void TakingANode_RaisesTheParameter_AndRefundingReturnsItExactly()
        {
            var fighter = new Fighter();
            IPassiveTreeService service = ShippedService(points: 5);
            fighter.Modifiers.RegisterSource(service.ParameterSource);
            fighter.Parameters.SetBaseValueForParameter(EntityParameter.Strength, BaseStrength);

            Assert.AreEqual(AllocationResult.Success, service.Take(FirstNode));
            Assert.AreEqual(BaseStrength + ChainStrengthPerNode, fighter[EntityParameter.Strength], 0f);

            Assert.AreEqual(AllocationResult.Success, service.Refund(FirstNode));
            Assert.AreEqual(BaseStrength, fighter[EntityParameter.Strength], 0f, "the refund must land exactly on the base value");
            Assert.AreEqual(0, service.SpentPoints);
        }

        [TestMethod]
        public void RefundingANode_LeavesTheContributionOfItemsOnTheSameParameterUntouched()
        {
            var fighter = new Fighter();
            IPassiveTreeService service = ShippedService(points: 5);
            var worn = new StubSource(new SimpleModifier(EntityParameter.Strength, ModifierValueType.Flat, 5f, "TestItem"));
            var loose = new SimpleModifier(EntityParameter.Strength, ModifierValueType.Flat, 3f, "TestBuff");

            fighter.Modifiers.RegisterSource(service.ParameterSource);
            fighter.Modifiers.RegisterSource(worn);
            fighter.Modifiers.AddModifier(loose);
            fighter.Parameters.SetBaseValueForParameter(EntityParameter.Strength, BaseStrength);
            float withoutTree = fighter[EntityParameter.Strength];

            service.Take(FirstNode);
            Assert.AreEqual(withoutTree + ChainStrengthPerNode, fighter[EntityParameter.Strength], 0f);

            service.Refund(FirstNode);

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

            service.Take(FirstNode);

            Assert.IsFalse(fighter.Modifiers.EntityModifiers.Values.SelectMany(lines => lines)
                    .Any(modifier => modifier.Source == PassiveTreeDocument.ModifierSource),
                "the tree wrote into the entity's own modifier list — that is the double-count channel");
            Assert.AreEqual(1, TreeLinesFor(fighter, EntityParameter.Strength),
                "one taken node with one Strength line must resolve to exactly one modifier");
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
            IPassiveTreeService service = ShippedService(points: 65);
            string[] unlinked = service.Tree.Nodes
                .Where(node => node.Kind != PassiveNodeKind.Start && service.Tree.Neighbours(node.Id).Count == 0)
                .Select(node => node.Id).ToArray();

            Assert.IsTrue(unlinked.Length > 0,
                "the shipped tree used to carry unlinked nodes (keystone_4, sockettier2_Increasing_Pressure_2) — this case needs one");

            foreach (string id in unlinked)
                Assert.AreEqual(AllocationResult.NotConnected, service.Take(id), id);
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
            service.Take(FirstNode);
            service.Take(SecondNode);
            service.Take(ThirdNode);

            Assert.AreEqual(AllocationResult.WouldOrphan, service.Refund(SecondNode), "refunding the middle strands the end");
            Assert.AreEqual(AllocationResult.Success, service.Refund(ThirdNode));
            Assert.AreEqual(AllocationResult.Success, service.Refund(SecondNode), "the middle becomes an end once the tail is gone");
            Assert.AreEqual(1, service.SpentPoints);
        }

        [TestMethod]
        public void GrantedPoints_CapWhatCanBeTaken()
        {
            IPassiveTreeService service = ShippedService(points: 1);

            Assert.AreEqual(AllocationResult.Success, service.Take(FirstNode));
            Assert.AreEqual(0, service.AvailablePoints);
            Assert.AreEqual(AllocationResult.NotEnoughPoints, service.Take(SecondNode));

            service.SetTotalPoints(2);

            Assert.AreEqual(AllocationResult.Success, service.Take(SecondNode));
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
            fighter.Modifiers.RegisterSource(service.ParameterSource);
            fighter.Parameters.SetBaseValueForParameter(EntityParameter.Strength, BaseStrength);
            fighter.Parameters.SetBaseValueForParameter(EntityParameter.Dexterity, BaseStrength);

            service.Take(FirstNode);
            service.Take(SecondNode);
            service.Take(ThirdNode);

            service.Respec();

            Assert.AreEqual(0, service.SpentPoints);
            Assert.AreEqual(BaseStrength, fighter[EntityParameter.Strength], 0f);
            Assert.AreEqual(BaseStrength, fighter[EntityParameter.Dexterity], 0f);
            Assert.AreEqual(0, TreeLinesFor(fighter, EntityParameter.Strength));
            Assert.AreEqual(0, TreeLinesFor(fighter, EntityParameter.Dexterity));
        }

        [TestMethod]
        public void TheTakenSetAnnouncesEveryChange()
        {
            IPassiveTreeService service = ShippedService(points: 5);
            Assert.IsTrue(service.TakenNodes.Count > 0, "adopting the loaded document puts the granted seeds in the set");

            int changes = 0;
            service.AllocationChanged += () => changes++;

            service.Take(FirstNode);
            service.Take("no_such_node");
            service.Refund(FirstNode);
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
            var service = new PassiveTreeService(provider);
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

            Assert.AreEqual(AllocationResult.Success, service.CheckTake(FirstNode));
            Assert.AreEqual(0, service.SpentPoints, "a check must not spend a point");
            Assert.IsFalse(service.IsTaken(FirstNode), "a check must not take the node");
            Assert.AreEqual(AllocationResult.UnknownNode, service.CheckTake("no_such_node"));

            service.Take(FirstNode);

            Assert.AreEqual(AllocationResult.AlreadyTaken, service.CheckTake(FirstNode));
            Assert.AreEqual(AllocationResult.NotEnoughPoints, service.CheckTake(SecondNode));
        }

        [TestMethod]
        public void TheSourceHandsOutSnapshots_NotTheCollectionsItRebuilds()
        {
            // RegisterSource walks AffectedParameters while announcing the source; a handler that ends
            // in another Rebuild would empty the live key set out from under that walk.
            IPassiveTreeService service = ShippedService(points: 5);
            service.Take(FirstNode);
            IReadOnlyCollection<EntityParameter> affected = service.ParameterSource.AffectedParameters;
            IEnumerable<IModifierInstance> lines = service.ParameterSource.GetModifiers(EntityParameter.Strength);

            service.Respec();

            Assert.IsTrue(affected.Count > 0, "the live key set was handed out — the rebuild emptied it mid-walk");
            Assert.AreEqual(1, lines.Count(), "the line list was handed out live — the rebuild changed it after the fact");
            Assert.IsFalse(lines is ICollection<IModifierInstance> { IsReadOnly: false },
                "the source handed out the list it edits — a caller could rewrite the tree's contribution in place");
        }

        private static EntityParameter[] Attributes =>
            [EntityParameter.Strength, EntityParameter.Dexterity, EntityParameter.Intelligence];

        private const string SyntheticNode = "small_1";

        private const string SyntheticSeed = "start";

        private static int TreeLinesFor(Fighter fighter, EntityParameter parameter) =>
            fighter.Modifiers.GetModifiers(parameter).Count(modifier => modifier.Source == PassiveTreeDocument.ModifierSource);

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

        private static IPassiveTreeService Service(PassiveTreeDocument document, int points)
        {
            var service = new PassiveTreeService(new TreeProviderStub(document));
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

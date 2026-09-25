namespace LastBreathTest.BattleSystemTests
{
    using Battle.Source;
    using Core.Battle.Abilities;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.PassiveTree;
    using Core.PassiveTree.Allocation;
    using Core.Services;

    /// <summary>
    /// Planning an allocation without paying for it. The claim the whole thing rests on is NEGATIVE: a
    /// marked node must reach nothing — no parameter, no point, no file — until the plan is applied, and
    /// it must reach everything the moment it is. The positive half of that is easy to get right by
    /// accident; the negative half is the one that costs a character bonuses he never bought.
    /// <para>Everything here stands on a hand-made tree. The shipped markup's ids move under an
    /// authoring pass, and a test naming one would break on a day nothing was wrong with the code.</para>
    /// </summary>
    [TestClass]
    public class PassiveTreeDraftTests
    {
        private const string Seed = "seed";
        private const string First = "first";
        private const string Second = "second";
        private const string Third = "third";
        private const string Branch = "branch";

        /// <summary>The ability the socket fixture hangs its slots on.</summary>
        private const string SeedAbility = "Ability_Seed";

        private const float NodeStrength = 2f;
        private const float BaseStrength = 10f;

        [TestMethod]
        public void AMarkedNode_SpendsNoPoint_AndReachesNoFighter()
        {
            var fighter = new Fighter();
            IPassiveTreeService service = Service(points: 3);
            fighter.Modifiers.RegisterSource(service.ParameterSource);
            fighter.Parameters.SetBaseValueForParameter(EntityParameter.Strength, BaseStrength);
            using var draft = new PassiveTreeDraft(service);

            Assert.AreEqual(AllocationResult.Success, draft.Mark(First));

            Assert.AreEqual(BaseStrength, fighter[EntityParameter.Strength], 0f, "a marked node paid out before it was bought");
            Assert.AreEqual(0, service.SpentPoints, "a marked node cost a point");
            Assert.AreEqual(3, service.AvailablePoints);
            Assert.IsFalse(service.IsTaken(First), "a marked node entered the allocation");
            CollectionAssert.DoesNotContain(service.TakenNodes.ToArray(), First,
                "a marked node is in the set the save participant writes");

            Assert.AreEqual(AllocationResult.Success, draft.ApplyTakes());

            Assert.AreEqual(BaseStrength + NodeStrength, fighter[EntityParameter.Strength], 0f);
            Assert.AreEqual(1, service.SpentPoints);
            Assert.IsTrue(service.IsTaken(First));
        }

        /// <summary>
        /// The same negative claim in the channel it is easiest to lose: a marked socket node opens no
        /// augment slot. The board is synced from the ALLOCATION and the plan lives outside it, so this
        /// holds because there is no channel and not because anybody agreed to it.
        /// </summary>
        [TestMethod]
        public void AMarkedSocketNode_OpensNoSlotUntilThePlanIsApplied()
        {
            var document = new PassiveTreeDocument();
            document.AddNode(new PassiveNode
            {
                Id = Seed,
                Kind = PassiveNodeKind.Start,
                Stance = Stance.Dexterity,
                AbilityId = SeedAbility
            });
            document.AddNode(new PassiveNode { Id = First, Kind = PassiveNodeKind.SocketTier2, AbilityId = SeedAbility });
            document.Link(Seed, First);

            var service = new PassiveTreeService(new TreeProviderStub(document), ConditionCatalogs.Empty());
            service.SetTotalPoints(3);
            var board = new AbilitySocketBoard();
            using var unlocks = new AbilityUnlockService(new PlayerAccessor(), new OneAbility(), service, board);
            using var draft = new PassiveTreeDraft(service);

            Assert.AreEqual(1, board.SocketsOf(SeedAbility).Count, "the fixture must start with the seed's own slot");

            Assert.AreEqual(AllocationResult.Success, draft.Mark(First));

            Assert.AreEqual(1, board.SocketsOf(SeedAbility).Count, "a marked node opened a slot nobody has paid for");
            Assert.IsNull(board.Find(new AbilitySocketPlacement(First, SeedAbility, 2).Address));

            Assert.AreEqual(AllocationResult.Success, draft.ApplyTakes());

            Assert.AreEqual(2, board.SocketsOf(SeedAbility).Count, "the applied plan did not open the slot");
        }

        /// <summary>The point of a projection rather than a list of intentions: the second step is legal
        /// only because the first one is in the plan, and there is no second body of rules saying so.</summary>
        [TestMethod]
        public void ANodeReachableOnlyThroughAnotherMark_CanBeMarked()
        {
            IPassiveTreeService service = Service(points: 3);
            using var draft = new PassiveTreeDraft(service);

            Assert.AreEqual(AllocationResult.NotConnected, draft.CanMark(Second), "the fixture must start out of reach");

            Assert.AreEqual(AllocationResult.Success, draft.Mark(First));
            Assert.AreEqual(AllocationResult.Success, draft.CanMark(Second));
            Assert.AreEqual(AllocationResult.Success, draft.Mark(Second));

            CollectionAssert.AreEqual(new[] { First, Second }, draft.PendingTakes.ToArray());
            Assert.AreEqual(2, draft.ProjectedSpent);
            Assert.AreEqual(1, draft.ProjectedAvailable);
        }

        /// <summary>A plan the budget cannot cover buys none of itself. Half a plan is never what was
        /// asked for, and the half that landed would be the cheap half.</summary>
        [TestMethod]
        public void APlanTheBudgetCannotCover_BuysNoneOfItself()
        {
            IPassiveTreeService service = Service(points: 3);
            using var draft = new PassiveTreeDraft(service);
            draft.MarkPath(service.PathTo(Third));

            Assert.AreEqual(3, draft.PendingTakes.Count, "the fixture must plan more than the budget will hold");
            service.SetTotalPoints(2);

            Assert.AreEqual(AllocationResult.NotEnoughPoints, draft.ApplyTakes());

            Assert.AreEqual(0, service.SpentPoints, "a refused plan bought part of itself");
            Assert.IsFalse(service.IsTaken(First));
        }

        /// <summary>The price under the cursor is priced from the PLAN: a step already marked is paid for
        /// by the plan and must not be charged twice.</summary>
        [TestMethod]
        public void TheRouteToANode_IsPricedFromThePlanAndNotFromTheCharacter()
        {
            IPassiveTreeService service = Service(points: 5);
            using var draft = new PassiveTreeDraft(service);

            Assert.AreEqual(3, service.PathTo(Third).Count);

            draft.Mark(First);
            draft.Mark(Second);

            CollectionAssert.AreEqual(new[] { Third }, draft.PathTo(Third).ToArray(),
                "the plan's own steps were charged for a second time");
            Assert.AreEqual(3, service.PathTo(Third).Count, "the plan moved the character's own price");
        }

        /// <summary>
        /// A return takes the node together with whatever would be left hanging behind it. The rule that
        /// unwinding happens from the ends is not lifted — the plan FINDS the ends instead of demanding
        /// them from the player — and the widening is not a bill nobody agreed to, because the size of
        /// the tail is named before the click (<see cref="PassiveTreeDraft.RefundTailOf"/>).
        /// </summary>
        [TestMethod]
        public void MarkingTheMiddleOfABranchForReturn_TakesTheTailWithIt()
        {
            IPassiveTreeService service = Service(points: 5);
            service.TakePath(service.PathTo(Third));
            using var draft = new PassiveTreeDraft(service) { Mode = DraftMode.Refund };

            CollectionAssert.AreEqual(new[] { Third, Second }, draft.RefundTailOf(Second).ToArray(),
                "the price named before the click is not the set the click takes, and it is ordered from the leaves");

            Assert.AreEqual(AllocationResult.Success, draft.CanMark(Second));
            Assert.AreEqual(AllocationResult.Success, draft.Mark(Second));

            CollectionAssert.AreEquivalent(new[] { Third, Second }, draft.PendingRefunds.ToArray(),
                "the tail was left hanging in the air");
            Assert.AreEqual(3, service.SpentPoints, "planning a return gave a point back");
            Assert.IsTrue(service.IsTaken(Second), "planning a return took the node off the character");
        }

        /// <summary>Dropping a mark is a replay without it, and a tail is no exception: without its head
        /// the tail no longer projects, so the plan falls apart from the leaf up. Taking the HEAD out
        /// leaves the tail standing — a branch end is a legal thing to give back on its own.</summary>
        [TestMethod]
        public void DroppingOneMarkOfAReturn_KeepsOnlyWhatStillHoldsWithoutIt()
        {
            IPassiveTreeService service = Service(points: 5);
            service.TakePath(service.PathTo(Third));
            using var draft = new PassiveTreeDraft(service) { Mode = DraftMode.Refund };
            draft.Mark(Second);

            draft.Unmark(Third);
            Assert.AreEqual(0, draft.PendingRefunds.Count, "the head of the plan outlived the tail it depended on");

            draft.Mark(Second);
            draft.Unmark(Second);

            CollectionAssert.AreEqual(new[] { Third }, draft.PendingRefunds.ToArray(),
                "dropping the head took the branch end with it, and the end is legal on its own");
        }

        /// <summary>Seeds come with the character and are never given back, so they cannot be planned
        /// away either.</summary>
        [TestMethod]
        public void ASeedCannotBeMarkedForReturn()
        {
            IPassiveTreeService service = Service(points: 5);
            using var draft = new PassiveTreeDraft(service) { Mode = DraftMode.Refund };

            Assert.AreEqual(AllocationResult.Granted, draft.CanMark(Seed));
            Assert.AreEqual(AllocationResult.NotTaken, draft.CanMark(First), "nothing but seeds is held yet");
        }

        /// <summary>Dropping a mark is a replay without it, so whatever stood only on that mark stops
        /// standing. One rule for letting a mark in and for letting it go.</summary>
        [TestMethod]
        public void DroppingAMark_DropsWhatOnlyStoodOnIt()
        {
            IPassiveTreeService service = Service(points: 5);
            using var draft = new PassiveTreeDraft(service);
            draft.MarkPath(service.PathTo(Third));
            draft.Mark(Branch);

            draft.Unmark(First);

            Assert.AreEqual(0, draft.PendingTakes.Count, "the whole plan hung off the first step and survived it");
            Assert.IsTrue(draft.IsEmpty);
        }

        /// <summary>A purchase made outside the plan — the wheel's own immediate buy, the console, a
        /// loaded file — rebases it. The node bought is simply no longer something to plan.</summary>
        [TestMethod]
        public void AnOutsidePurchase_RebasesThePlanAndKeepsTheRestOfIt()
        {
            IPassiveTreeService service = Service(points: 5);
            using var draft = new PassiveTreeDraft(service);
            draft.MarkPath(service.PathTo(Third));

            Assert.AreEqual(AllocationResult.Success, service.Take(First));

            CollectionAssert.AreEqual(new[] { Second, Third }, draft.PendingTakes.ToArray(),
                "the bought node stayed in the plan, or took the rest of it with it");
            Assert.IsFalse(draft.IsMarked(First), "the plan still means to buy a node the character now owns");
            CollectionAssert.AreEqual(new[] { Second, Third }, draft.Marked.ToArray(),
                "the marks were not replayed onto the allocation that moved under them");
            Assert.AreEqual(3, draft.ProjectedSpent, "the plan is counted against the bought node twice");
        }

        /// <summary>The tree is authored daily. A plan made against yesterday's document keeps what the
        /// new one still knows and quietly loses what it does not — the same treatment a saved allocation
        /// gets, and for the same reason.</summary>
        [TestMethod]
        public void ADocumentSwappedUnderThePlan_KeepsOnlyWhatSurvivedTheEdit()
        {
            var provider = new TreeProviderStub(Tree());
            var service = new PassiveTreeService(provider, ConditionCatalogs.Empty());
            service.SetTotalPoints(5);
            using var draft = new PassiveTreeDraft(service);
            draft.MarkPath(service.PathTo(Third));

            provider.Tree = Tree(withTail: false);
            _ = service.Tree; // the catalog reload the game performs on its own

            CollectionAssert.AreEqual(new[] { First }, draft.PendingTakes.ToArray(),
                "the plan kept nodes the new document no longer holds");
        }

        [TestMethod]
        public void SwitchingModeClearsThePlan_AndClosingItBuysNothing()
        {
            IPassiveTreeService service = Service(points: 5);
            using var draft = new PassiveTreeDraft(service);
            draft.Mark(First);

            draft.Mode = DraftMode.Refund;

            Assert.IsTrue(draft.IsEmpty, "a plan to buy survived into the mode that gives things back");
            Assert.AreEqual(0, draft.PendingTakes.Count);

            draft.Mode = DraftMode.Take;
            draft.Mark(First);
            draft.Clear();

            Assert.IsTrue(draft.IsEmpty);
            Assert.AreEqual(0, service.SpentPoints, "closing a plan bought part of it");
            Assert.IsFalse(service.IsTaken(First));
        }

        /// <summary>A plan that gives nodes back buys nothing, whatever button is pressed: the paid road
        /// is the respec gate, and this one has to refuse rather than reach for the tree.</summary>
        [TestMethod]
        public void APlanToGiveNodesBack_BuysNothing()
        {
            IPassiveTreeService service = Service(points: 5);
            service.TakePath(service.PathTo(Second));
            using var draft = new PassiveTreeDraft(service) { Mode = DraftMode.Refund };
            draft.Mark(Second);

            Assert.AreEqual(AllocationResult.NotConnected, draft.ApplyTakes());
            Assert.AreEqual(2, service.SpentPoints);
            CollectionAssert.AreEqual(new[] { Second }, draft.PendingRefunds.ToArray(), "the plan lost its marks to a wrong button");
        }

        private static IPassiveTreeService Service(int points)
        {
            var service = new PassiveTreeService(new TreeProviderStub(Tree()), ConditionCatalogs.Empty());
            service.SetTotalPoints(points);
            return service;
        }

        /// <summary>A seed with a three-node chain hanging off it and one node branching off the first
        /// step, every node carrying the same flat line so a bonus that arrived early is visible.</summary>
        private static PassiveTreeDocument Tree(bool withTail = true)
        {
            var document = new PassiveTreeDocument();
            document.AddNode(new PassiveNode { Id = Seed, Kind = PassiveNodeKind.Start, Stance = Stance.Strength });
            Add(First, 10f);
            document.Link(Seed, First);

            Add(Branch, 10f);
            document.Link(First, Branch);

            if (withTail)
            {
                Add(Second, 20f);
                Add(Third, 30f);
                document.Link(First, Second);
                document.Link(Second, Third);
            }

            return document;

            void Add(string id, float x)
            {
                var node = new PassiveNode { Id = id, Kind = PassiveNodeKind.Small, X = x };
                node.Modifiers.Add(new ModifierLine
                {
                    Parameter = EntityParameter.Strength,
                    ValueType = ModifierValueType.Flat,
                    Value = NodeStrength
                });
                document.AddNode(node);
            }
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

        /// <summary>Stands in for the data pipeline, including a reload: the document can be swapped the
        /// way a catalog reload swaps it.</summary>
        private sealed class TreeProviderStub(PassiveTreeDocument tree) : IPassiveTreeProvider
        {
            public PassiveTreeDocument Tree { get; set; } = tree;

            public IReadOnlyList<string> Issues => [];
        }

        /// <summary>A catalog holding the one ability the socket fixture names. Nothing here builds an
        /// ability: the walk composes no player, so the book is never filled and only the socket half of
        /// the unlock pass runs.</summary>
        private sealed class OneAbility : IAbilityProvider
        {
            public IReadOnlyCollection<string> KnownAbilityIds => [SeedAbility];

            public IAbility CreateAbility(string abilityId) =>
                throw new NotSupportedException("no player in this walk, so nothing is ever learned");

            public IAugment? CreateUpgrade(AugmentInstance augment) => null;

            public Stance GetAbilityStance(string abilityId) => Stance.Dexterity;

            public bool IsHidden(string abilityId) => false;
        }
    }
}

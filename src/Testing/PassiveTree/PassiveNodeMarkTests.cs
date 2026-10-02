namespace LastBreathTest.PassiveTree
{
    using Core.Enums;
    using Core.Localization;
    using Core.PassiveTree;
    using Core.PassiveTree.Allocation;
    using Core.PassiveTree.View;
    using Localization;

    /// <summary>
    /// What the wheel shows for a plan nobody has paid for yet.
    /// <para>The claim under test is the one a player notices first: a click MARKS, so a marked node has
    /// to look like something. Marked-to-buy and marked-to-give-back are opposite acts and must not look
    /// alike, and neither may look like what the character already holds or like a node nobody has
    /// touched — mistaking one for the other is paying for the wrong thing.</para>
    /// <para>The rule is checked as ONE rule. The picture is assembled by two mechanisms — the nodes that
    /// carry a scene of their own paint themselves, everything else is drawn by a layer over the whole
    /// field — and a rule spelled once per mechanism is exactly how a mark comes out two different ways,
    /// or comes out on one class of node and not on another.</para>
    /// </summary>
    [TestClass]
    public class PassiveNodeMarkTests
    {
        private const string Seed = "seed";
        private const string First = "first";
        private const string Second = "second";
        private const string Third = "third";

        /// <summary>The class the look draws as part of the mass instead of giving it a scene. Most of
        /// any road is made of it, so it is the class a mark must not skip.</summary>
        private const PassiveNodeKind Mass = PassiveNodeKind.Small;

        /// <summary>A class that carries a scene of its own.</summary>
        private const PassiveNodeKind Carrier = PassiveNodeKind.Notable;

        /// <summary>The catalog entry that joins a line to the clause gating it. Spelled out here because
        /// the join is what is under test — a fixture without it would word nothing at all.</summary>
        private const string ConditionalTemplate = "Modifier_Conditional";

        /// <summary>The four things a node can be, and four pictures for them. Held-and-planned is owned,
        /// planned-without-being-held is a purchase, held-without-being-planned is a return.</summary>
        [TestMethod]
        public void WhatANodeIsToThePlan_HasAPictureOfItsOwnForEachAnswer()
        {
            PassiveNodeVisualState taken = PassiveNodeStates.Of(taken: true, projected: true, onPath: false);
            PassiveNodeVisualState buying = PassiveNodeStates.Of(taken: false, projected: true, onPath: false);
            PassiveNodeVisualState giving = PassiveNodeStates.Of(taken: true, projected: false, onPath: false);
            PassiveNodeVisualState idle = PassiveNodeStates.Of(taken: false, projected: false, onPath: false);

            Assert.AreEqual(PassiveNodeVisualState.Taken, taken);
            Assert.AreEqual(PassiveNodeVisualState.Pending, buying);
            Assert.AreEqual(PassiveNodeVisualState.PendingRefund, giving);
            Assert.AreEqual(PassiveNodeVisualState.Idle, idle);

            Assert.AreEqual(4, new HashSet<PassiveNodeVisualState> { taken, buying, giving, idle }.Count,
                "two of the four things a node can be answer with the same picture");
        }

        /// <summary>
        /// The marks themselves. Both plans wear one, the two are told apart by more than a hue — they are
        /// opposite acts and a player who reads colour badly still has to see which one he is about to pay
        /// for — and neither is worn by a node the plan does not touch.
        /// </summary>
        [TestMethod]
        public void BothPlansWearAMark_TheyDiffer_AndNothingElseWearsOne()
        {
            PassiveNodeMark buying = PassiveNodeStates.MarkOf(PassiveNodeVisualState.Pending);
            PassiveNodeMark giving = PassiveNodeStates.MarkOf(PassiveNodeVisualState.PendingRefund);

            Assert.IsTrue(buying.IsDrawn, "a node marked to be bought is drawn exactly like one nobody has touched");
            Assert.IsTrue(giving.IsDrawn, "a node marked to be given back is drawn exactly like one nobody has touched");
            Assert.AreNotEqual(buying, giving, "a purchase and a return are the same picture");
            Assert.AreNotEqual(buying.RingScale, giving.RingScale,
                "the two marks are told apart by colour alone, which is not a shape");

            Assert.IsFalse(PassiveNodeStates.MarkOf(PassiveNodeVisualState.Taken).IsDrawn,
                "what is already held wears the plan's mark");
            Assert.IsFalse(PassiveNodeStates.MarkOf(PassiveNodeVisualState.Idle).IsDrawn);
            Assert.IsFalse(PassiveNodeStates.MarkOf(PassiveNodeVisualState.OnPath).IsDrawn,
                "a step of the previewed route is not something the plan has been told to buy");
        }

        /// <summary>
        /// Over a live plan, and read the two ways the window reads it: node by node, the way a node scene
        /// is told what it is, and once for the whole plan, the way the layer that marks the field is. The
        /// two have to agree on every node and on every class — the mass is not allowed to be the class
        /// whose marks go missing.
        /// </summary>
        [TestMethod]
        public void EveryNodeAPlanWouldBuy_WearsTheSameMark_WhateverClassItIs()
        {
            IPassiveTreeService service = Service(points: 5);
            using var draft = new PassiveTreeDraft(service);
            draft.MarkPath(service.PathTo(Third));

            Assert.AreEqual(3, draft.PendingTakes.Count, "the fixture must plan a road of more than one step");

            PassiveNodeVisualState wholePlan = PassiveNodeStates.OfPlanned(givesBack: false);
            Assert.IsTrue(PassiveNodeStates.MarkOf(wholePlan).IsDrawn);

            var classes = new HashSet<PassiveNodeKind>();
            foreach (string id in draft.PendingTakes)
            {
                Assert.AreEqual(wholePlan, StateOf(service, draft, id),
                    $"{id} is one thing to its own scene and another to the layer that marks the field");
                classes.Add(service.Tree.Find(id)!.Kind);
            }

            CollectionAssert.Contains(classes.ToArray(), Mass, "the fixture never marked a node of the mass class");
            CollectionAssert.Contains(classes.ToArray(), Carrier, "the fixture never marked a node that carries a scene");
        }

        /// <summary>The same, pointing the other way: everything a return plans wears the return's mark,
        /// and it is not the purchase's.</summary>
        [TestMethod]
        public void EveryNodeAPlanWouldGiveBack_WearsTheReturnsMark()
        {
            IPassiveTreeService service = Service(points: 5);
            service.TakePath(service.PathTo(Third));
            using var draft = new PassiveTreeDraft(service) { Mode = DraftMode.Refund };
            draft.Mark(First);

            Assert.AreEqual(3, draft.PendingRefunds.Count, "the fixture must plan back more than one node");

            PassiveNodeVisualState wholePlan = PassiveNodeStates.OfPlanned(givesBack: true);
            Assert.AreEqual(PassiveNodeStates.MarkOf(PassiveNodeVisualState.PendingRefund), PassiveNodeStates.MarkOf(wholePlan));
            Assert.AreNotEqual(PassiveNodeStates.MarkOf(PassiveNodeVisualState.Pending), PassiveNodeStates.MarkOf(wholePlan),
                "a plan that gives nodes back is drawn as a plan that buys them");

            foreach (string id in draft.PendingRefunds)
                Assert.AreEqual(wholePlan, StateOf(service, draft, id),
                    $"{id} is one thing to its own scene and another to the layer that marks the field");
        }

        /// <summary>A node whose line is held up by something says so in words. The clause is worded under
        /// the condition's own catalog key, the way every other surface with conditional lines words it, so
        /// what the popup prints is a sentence and never the id the tree file names the gate by.</summary>
        [TestMethod]
        public void AConditionalLine_PrintsTheCatalogsClause_AndNotTheRawConditionId()
        {
            const string condition = "Stance_Strength";
            const string clause = "while holding the stance of force";

            var provider = new FakeLocalizationProvider();
            provider.Strings[ConditionalLineText.ClauseKey(condition)] = clause;
            provider.Strings[ConditionalTemplate] = "{line} ({condition})";

            var node = new PassiveNode { Id = First, Kind = Carrier };
            node.Modifiers.Add(new ModifierLine
            {
                Parameter = EntityParameter.Strength,
                ValueType = ModifierValueType.Flat,
                Value = 5f,
                Condition = condition
            });

            List<PassiveNodeLine> lines = PassiveNodeLines.Of(node, null, null, provider);

            Assert.AreEqual(1, lines.Count);
            Assert.IsTrue(lines[0].IsConditional, "the line lost the mark that says something holds it up");
            StringAssert.Contains(lines[0].Text, clause, "the node named its gate with no words at all");
            Assert.IsFalse(lines[0].Text.Contains(condition, StringComparison.Ordinal),
                $"the node printed the raw condition id: {lines[0].Text}");

            // The wheel's popup is the rich reading, and a clause is painted apart from the sentence it
            // gates there — the plain one above would pass on a node that never reached the rich road.
            List<PassiveNodeLine> rich = PassiveNodeLines.Of(node, null, null, provider, TextFormat.Rich);

            StringAssert.Contains(rich[0].Text, TextPalette.Colorize(clause, TextPalette.Muted),
                $"the popup's clause arrived unpainted: {rich[0].Text}");
        }

        /// <summary>The window's own reading, node by node — the one a node scene is painted from.</summary>
        private static PassiveNodeVisualState StateOf(IPassiveTreeService service, PassiveTreeDraft draft, string id) =>
            PassiveNodeStates.Of(service.IsTaken(id), draft.IsProjected(id), onPath: false);

        private static IPassiveTreeService Service(int points)
        {
            var service = new PassiveTreeService(new TreeProviderStub(Tree()), ConditionCatalogs.Empty());
            service.SetTotalPoints(points);
            return service;
        }

        /// <summary>A seed with a three-node chain hanging off it, mixing the class that carries a scene
        /// with the class drawn as part of the mass — the whole point being that a mark treats them
        /// alike.</summary>
        private static PassiveTreeDocument Tree()
        {
            var document = new PassiveTreeDocument();
            document.AddNode(new PassiveNode { Id = Seed, Kind = PassiveNodeKind.Start, Stance = Stance.Strength });
            document.AddNode(new PassiveNode { Id = First, Kind = Mass, X = 10f });
            document.AddNode(new PassiveNode { Id = Second, Kind = Mass, X = 20f });
            document.AddNode(new PassiveNode { Id = Third, Kind = Carrier, X = 30f });
            document.Link(Seed, First);
            document.Link(First, Second);
            document.Link(Second, Third);

            return document;
        }

        private sealed class TreeProviderStub(PassiveTreeDocument tree) : IPassiveTreeProvider
        {
            public PassiveTreeDocument Tree { get; } = tree;

            public IReadOnlyList<string> Issues => [];
        }
    }
}

namespace LastBreathTest.BattleSystemTests
{
    using Core.Battle.Skills;
    using Core.Enums;
    using Core.PassiveTree;
    using Core.PassiveTree.Editing;

    /// <summary>
    /// Editing several nodes of the tree at once. The author lays down five small stat nodes and then wants
    /// one title, one stance and one number on all of them — and the two ways that can go wrong are both
    /// invisible on screen until the file is already written.
    /// <para>The first is a group edit that quietly hands one node's value to the rest: a field the nodes
    /// disagree on has no common value, and a panel that showed one anyway would overwrite four nodes'
    /// balance with the fifth's. The second is a batch that only reaches part of the selection — the author
    /// sees the field he typed into and never learns that four nodes stayed as they were.</para>
    /// <para>Undo is the third: the gesture was one thing the author did, so it has to be one thing he can
    /// take back, and every node has to come back to the value it — not its neighbour — had.</para>
    /// </summary>
    [TestClass]
    public class NodeGroupEditTests
    {
        private const string StatPoison = StatPassiveGrammar.IdPrefix + "Poison";
        private const string StatFangs = StatPassiveGrammar.IdPrefix + "Fangs";
        private const string NamedPassive = "Passive_Skill_Poisoned_Claws";
        private const string StrengthFlat = "Strength:Flat";
        private const string EvadeIncrease = "Evade:Increase";

        /// <summary>Five small stat nodes are the same node five times over: one class, one passive family,
        /// one set of field names. The ids of two stat passives differ by design — the family is what its
        /// fields say, so nodes named apart are still edited together.</summary>
        [TestMethod]
        public void NodesOfOneClassAndOneFieldSetAreOneGroup()
        {
            PassiveNode first = Stat(StatPoison, (StrengthFlat, 3f));
            PassiveNode second = Stat(StatFangs, (StrengthFlat, 5f));

            Assert.IsTrue(NodeGroups.SameShape(first, second),
                "two stat nodes writing the same field are the same shape whatever each is called");

            Assert.IsTrue(NodeGroups.Uniform([first, second]));
        }

        /// <summary>The three ways a selection stops being one group. Each of them means a row would say
        /// something different on one node than on another, which is exactly when the panel has to stop
        /// offering the row at all.</summary>
        [TestMethod]
        public void ADifferentClassFamilyOrFieldSetBreaksTheGroup()
        {
            PassiveNode node = Stat(StatPoison, (StrengthFlat, 3f));

            PassiveNode otherKind = Stat(StatPoison, (StrengthFlat, 3f));
            otherKind.Kind = PassiveNodeKind.Notable;

            PassiveNode otherFamily = Named(NamedPassive, (StrengthFlat, 3f));
            PassiveNode otherFields = Stat(StatPoison, (EvadeIncrease, 3f));

            Assert.IsFalse(NodeGroups.SameShape(node, otherKind), "a class decides which fields a node has");
            Assert.IsFalse(NodeGroups.SameShape(node, otherFamily), "a named passive's factory reads its own names");
            Assert.IsFalse(NodeGroups.SameShape(node, otherFields), "a row must mean the same field on every node");
            Assert.IsFalse(NodeGroups.Uniform([node, otherKind, otherFamily]));
        }

        /// <summary>The order a node wrote its record in is its own. Two nodes carrying the same names in a
        /// different order are still the same shape — a set, never a list.</summary>
        [TestMethod]
        public void FieldOrderIsEachNodesOwnAndDoesNotSplitTheGroup()
        {
            PassiveNode first = Stat(StatPoison, (StrengthFlat, 1f), (EvadeIncrease, 2f));
            PassiveNode second = Stat(StatPoison, (EvadeIncrease, 2f), (StrengthFlat, 1f));

            Assert.IsTrue(NodeGroups.SameShape(first, second));
        }

        /// <summary>A field the nodes disagree on has no value to show. This is the pin the whole section
        /// stands on: mixed has to survive the merge, or the panel offers the first node's text as
        /// everybody's and the author overwrites four titles without seeing it happen.</summary>
        [TestMethod]
        public void AFieldTheNodesDisagreeOnComesBackMixed()
        {
            PassiveNode first = Stat(StatPoison, (StrengthFlat, 3f));
            PassiveNode second = Stat(StatPoison, (StrengthFlat, 3f));

            first.Title = "Bite";
            second.Title = "Claw";
            first.Stance = Stance.Strength;
            second.Stance = Stance.Strength;

            MergedValue<string> title = NodeGroups.Merge([first, second], node => node.Title, StringComparer.Ordinal);
            MergedValue<Stance?> stance = NodeGroups.Merge<Stance?>([first, second], node => node.Stance);

            Assert.IsTrue(title.Mixed, "two titles are not one title");
            Assert.IsFalse(stance.Mixed);
            Assert.AreEqual(Stance.Strength, stance.Value);
        }

        /// <summary>The same rule one field down: a number the nodes write differently is mixed, and the row
        /// carries no number of its own to be typed over by accident.</summary>
        [TestMethod]
        public void ANumberTheNodesWriteDifferentlyIsMixed()
        {
            PassiveNode first = Stat(StatPoison, (StrengthFlat, 3f), (EvadeIncrease, 0.1f));
            PassiveNode second = Stat(StatPoison, (StrengthFlat, 5f), (EvadeIncrease, 0.1f));

            List<MergedProperty> merged = NodeGroups.MergeProperties([first, second]);

            Assert.AreEqual(2, merged.Count);
            Assert.AreEqual(StrengthFlat, merged[0].Key, "the first node's order is the order the rows stand in");
            Assert.IsTrue(merged[0].Mixed, "3 and 5 are not one number");
            Assert.AreEqual(0f, merged[0].Value, "a mixed row offers no value of its own");
            Assert.IsFalse(merged[1].Mixed);
            Assert.AreEqual(0.1f, merged[1].Value, 0.0001f);
        }

        /// <summary>A key one node does not carry is no row of the group: touching it would write a field
        /// into a node that never had it.</summary>
        [TestMethod]
        public void AKeyNotEveryNodeCarriesIsNoRowOfTheGroup()
        {
            PassiveNode first = Stat(StatPoison, (StrengthFlat, 3f), (EvadeIncrease, 0.1f));
            PassiveNode second = Stat(StatPoison, (StrengthFlat, 3f));

            List<MergedProperty> merged = NodeGroups.MergeProperties([first, second]);

            Assert.AreEqual(1, merged.Count);
            Assert.AreEqual(StrengthFlat, merged[0].Key);
        }

        /// <summary>The batch reaches every node it names — the failure the author cannot see, because the
        /// field he typed into is showing what he typed whether four nodes followed it or none did. A node
        /// already at the value is left out of the plan rather than recorded as a step that moves nothing.</summary>
        [TestMethod]
        public void ABatchReachesEveryNodeThatDiffers()
        {
            PassiveNode first = Stat(StatPoison);
            PassiveNode second = Stat(StatPoison);
            PassiveNode third = Stat(StatPoison);

            first.Title = "Bite";
            second.Title = "Claw";
            third.Title = "Fang";

            List<NodeValueChange<string>> plan =
                NodeBatch.Plan([first, second, third], node => node.Title, "Fang", StringComparer.Ordinal);

            Assert.AreEqual(2, plan.Count, "the node already saying it is no part of the step");

            NodeBatch.Apply(plan, (node, title) => node.Title = title, forward: true);

            Assert.AreEqual("Fang", first.Title);
            Assert.AreEqual("Fang", second.Title);
            Assert.AreEqual("Fang", third.Title);
        }

        /// <summary>One gesture, one step back — and every node back to the value it had, not to its
        /// neighbour's.</summary>
        [TestMethod]
        public void OneStepBackReturnsEveryNodeToItsOwnValue()
        {
            PassiveNode first = Stat(StatPoison);
            PassiveNode second = Stat(StatPoison);

            first.Title = "Bite";
            second.Title = "Claw";

            List<NodeValueChange<string>> plan =
                NodeBatch.Plan([first, second], node => node.Title, "Fang", StringComparer.Ordinal);

            NodeBatch.Apply(plan, (node, title) => node.Title = title, forward: true);
            NodeBatch.Apply(plan, (node, title) => node.Title = title, forward: false);

            Assert.AreEqual("Bite", first.Title);
            Assert.AreEqual("Claw", second.Title);
        }

        /// <summary>Every node is written from its own entry, so the order the plan stands in cannot change
        /// what the tree ends up saying — forwards or back.</summary>
        [TestMethod]
        public void TheOrderOfThePlanChangesNothing()
        {
            PassiveNode first = Stat(StatPoison);
            PassiveNode second = Stat(StatPoison);

            first.Title = "Bite";
            second.Title = "Claw";

            List<NodeValueChange<string>> plan =
                NodeBatch.Plan([first, second], node => node.Title, "Fang", StringComparer.Ordinal);

            List<NodeValueChange<string>> reversed = [.. plan];
            reversed.Reverse();

            NodeBatch.Apply(reversed, (node, title) => node.Title = title, forward: true);
            Assert.AreEqual("Fang", first.Title);
            Assert.AreEqual("Fang", second.Title);

            NodeBatch.Apply(reversed, (node, title) => node.Title = title, forward: false);
            Assert.AreEqual("Bite", first.Title);
            Assert.AreEqual("Claw", second.Title);
        }

        /// <summary>
        /// A run of keystrokes on a group is one step whichever nodes each letter happened to move. The
        /// second plan is folded into the first: every node keeps the title it had before the run began, and
        /// a node that only joined at the second letter — it already said "F" — joins with its own.
        /// </summary>
        [TestMethod]
        public void AMergedRunKeepsWhatEveryNodeStartedFrom()
        {
            PassiveNode first = Stat(StatPoison);
            PassiveNode second = Stat(StatPoison);

            first.Title = "Bite";
            second.Title = "F";

            List<NodeValueChange<string>> run = NodeBatch.Plan([first, second], node => node.Title, "F", StringComparer.Ordinal);
            NodeBatch.Apply(run, (node, title) => node.Title = title, forward: true);
            Assert.AreEqual(1, run.Count, "the node already saying \"F\" was not moved by the first keystroke");

            List<NodeValueChange<string>> next = NodeBatch.Plan([first, second], node => node.Title, "Fa", StringComparer.Ordinal);
            NodeBatch.Apply(next, (node, title) => node.Title = title, forward: true);
            NodeBatch.Absorb(run, next);

            Assert.AreEqual(2, run.Count, "the second keystroke moved both nodes, so the step holds both");

            NodeBatch.Apply(run, (node, title) => node.Title = title, forward: false);
            Assert.AreEqual("Bite", first.Title, "one step back, and the node is at what it said before the run");
            Assert.AreEqual("F", second.Title, "the node that joined later comes back to its own state");
        }

        /// <summary>A number written across the group leaves every other field of every node exactly where
        /// it was — same names, same numbers, same order.</summary>
        [TestMethod]
        public void APropertyBatchTouchesOneKeyAndKeepsEachRecordsOrder()
        {
            PassiveNode first = Stat(StatPoison, (StrengthFlat, 3f), (EvadeIncrease, 0.1f));
            PassiveNode second = Stat(StatPoison, (StrengthFlat, 5f), (EvadeIncrease, 0.2f));

            List<NodeValueChange<List<KeyValuePair<string, float>>>> plan =
                NodeBatch.PlanPropertyValue([first, second], StrengthFlat, 7f);

            Assert.AreEqual(2, plan.Count);
            NodeBatch.Apply(plan, (node, rows) => node.SetProperties(rows), forward: true);

            Assert.AreEqual(7f, first.Properties[StrengthFlat]);
            Assert.AreEqual(7f, second.Properties[StrengthFlat]);
            Assert.AreEqual(0.1f, first.Properties[EvadeIncrease], 0.0001f, "the other field is nobody's business here");
            Assert.AreEqual(0.2f, second.Properties[EvadeIncrease], 0.0001f);

            CollectionAssert.AreEqual(new[] { StrengthFlat, EvadeIncrease },
                first.PropertyRows().Select(row => row.Key).ToArray(),
                "the record keeps the order its author wrote it in");

            NodeBatch.Apply(plan, (node, rows) => node.SetProperties(rows), forward: false);
            Assert.AreEqual(3f, first.Properties[StrengthFlat]);
            Assert.AreEqual(5f, second.Properties[StrengthFlat]);
        }

        /// <summary>Respelling a stat line across the group renames the key on every node and leaves each
        /// node's own number under it, in the place the old name held.</summary>
        [TestMethod]
        public void ARespellRenamesTheKeyAndKeepsEveryNodesNumber()
        {
            PassiveNode first = Stat(StatPoison, (StrengthFlat, 3f), (EvadeIncrease, 0.1f));
            PassiveNode second = Stat(StatPoison, (StrengthFlat, 5f), (EvadeIncrease, 0.2f));

            List<NodeValueChange<List<KeyValuePair<string, float>>>> plan =
                NodeBatch.PlanPropertyKey([first, second], StrengthFlat, "Dexterity:Flat");

            NodeBatch.Apply(plan, (node, rows) => node.SetProperties(rows), forward: true);

            Assert.AreEqual(3f, first.Properties["Dexterity:Flat"]);
            Assert.AreEqual(5f, second.Properties["Dexterity:Flat"]);
            Assert.IsFalse(first.Properties.ContainsKey(StrengthFlat));

            CollectionAssert.AreEqual(new[] { "Dexterity:Flat", EvadeIncrease },
                first.PropertyRows().Select(row => row.Key).ToArray(), "the renamed field stays where it stood");
        }

        /// <summary>A rename onto a key a node already writes is refused whole. Two rows under one name are
        /// one number, and which of them survived would be a coin toss — on every node at once.</summary>
        [TestMethod]
        public void ARespellOntoAKeySomeNodeAlreadyWritesIsRefusedForTheWholeGroup()
        {
            PassiveNode first = Stat(StatPoison, (StrengthFlat, 3f));
            PassiveNode second = Stat(StatPoison, (StrengthFlat, 5f), (EvadeIncrease, 0.2f));

            List<NodeValueChange<List<KeyValuePair<string, float>>>> plan =
                NodeBatch.PlanPropertyKey([first, second], StrengthFlat, EvadeIncrease);

            Assert.AreEqual(0, plan.Count, "nothing is written when one node cannot take the rename");
            Assert.AreEqual(3f, first.Properties[StrengthFlat], "and the node that could is left alone too");
        }

        private static PassiveNode Stat(string passiveId, params (string Key, float Value)[] fields) =>
            Build(passiveId, fields);

        private static PassiveNode Named(string passiveId, params (string Key, float Value)[] fields) =>
            Build(passiveId, fields);

        private static PassiveNode Build(string passiveId, (string Key, float Value)[] fields)
        {
            var node = new PassiveNode { Id = Guid.NewGuid().ToString("N"), PassiveId = passiveId };
            foreach ((string key, float value) in fields) node.Properties[key] = value;

            return node;
        }
    }
}

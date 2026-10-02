namespace LastBreathTest.PassiveTree
{
    using Core.PassiveTree;

    /// <summary>
    /// A passive's numbers are hand-balanced and read in the order they were written. The authoring tool
    /// edits them as a list and hands the whole of it back, because a dictionary gives a fresh key the
    /// slot a removed one left behind — patch it key by key and a field added after a removal silently
    /// takes the removed one's place in the file, re-diffing a document nobody meant to reorder.
    /// </summary>
    [TestClass]
    public class PassiveNodePropertyOrderTests
    {
        private const string PoisonedClaws = "Passive_Skill_Poisoned_Claws";

        [TestMethod]
        public void AFieldAddedAfterARemovalStandsAtTheEnd()
        {
            PassiveNode node = Node(("percentFromDamage", 0.75f), ("duration", 3f), ("maxStacks", 5f));

            List<KeyValuePair<string, float>> rows = node.PropertyRows();
            rows.RemoveAt(1);
            node.SetProperties(rows);

            rows = node.PropertyRows();
            rows.Add(new KeyValuePair<string, float>("duration", 4f));
            node.SetProperties(rows);

            CollectionAssert.AreEqual(new[] { "percentFromDamage", "maxStacks", "duration" }, node.Properties.Keys.ToArray(),
                "a field written after a removal belongs at the end, not in the hole the removal left");
        }

        [TestMethod]
        public void RenamingAFieldLeavesItWhereTheAuthorPutIt()
        {
            PassiveNode node = Node(("percentFromDamage", 0.75f), ("duraton", 3f), ("maxStacks", 5f));

            List<KeyValuePair<string, float>> rows = node.PropertyRows();
            rows[1] = new KeyValuePair<string, float>("duration", rows[1].Value);
            node.SetProperties(rows);

            CollectionAssert.AreEqual(new[] { "percentFromDamage", "duration", "maxStacks" }, node.Properties.Keys.ToArray(),
                "a typo corrected is the same field, and it stays on the line it was written on");

            Assert.AreEqual(3f, node.Properties["duration"], "the number travels with the name it was written under");
        }

        [TestMethod]
        public void TheAuthoredOrderSurvivesTheFile()
        {
            var document = new PassiveTreeDocument();
            document.AddNode(Passive(Node(("percentFromDamage", 0.75f), ("duration", 3f))));

            List<string> issues = [];
            PassiveTreeDocument reloaded =
                PassiveTreeSerializer.Deserialize(PassiveTreeSerializer.Serialize(document), issues);

            Assert.AreEqual(0, issues.Count, string.Join("; ", issues));
            CollectionAssert.AreEqual(new[] { "percentFromDamage", "duration" }, reloaded.Nodes[0].Properties.Keys.ToArray());
        }

        /// <summary>The writer canonicalises every number the same way coordinates are canonicalised, so a
        /// whole number authored by hand comes back with its decimal point. Not a loss — it is what makes a
        /// save after a load a no-op in git from the second save on.</summary>
        [TestMethod]
        public void AWholeNumberIsWrittenBackWithItsDecimalPoint()
        {
            var document = new PassiveTreeDocument();
            document.AddNode(Passive(Node(("duration", 3f))));

            StringAssert.Contains(PassiveTreeSerializer.Serialize(document), "\"duration\": 3.0");
        }

        private static PassiveNode Node(params (string Name, float Value)[] properties)
        {
            var node = new PassiveNode { Id = "keystone_1", Kind = PassiveNodeKind.Keystone };
            foreach ((string name, float value) in properties) node.Properties[name] = value;

            return node;
        }

        private static PassiveNode Passive(PassiveNode node)
        {
            node.PassiveId = PoisonedClaws;
            return node;
        }
    }
}

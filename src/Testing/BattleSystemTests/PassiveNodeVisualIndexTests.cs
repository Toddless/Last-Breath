namespace LastBreathTest.BattleSystemTests
{
    using Core.PassiveTree;
    using Core.PassiveTree.View;

    /// <summary>
    /// The choice between two authored looks, which is the whole of what the node-visual channel decides
    /// on its own: a node's own row, then its class's row, then nothing at all. Nothing is the answer the
    /// wheel is built around — it means "draw what you drew before there was a library" — so it is
    /// asserted as hard as the other two.
    /// <para>The rows here are a stand-in rather than the game's resource: what is under test is the
    /// address, and the Godot resource is a bag of textures hanging off it.</para>
    /// </summary>
    [TestClass]
    public class PassiveNodeVisualIndexTests
    {
        [TestMethod]
        public void ANodesOwnRow_WinsOverTheRowOfItsClass()
        {
            var index = Over(
                new Row(string.Empty, PassiveNodeKind.Notable, "class"),
                new Row("notable_7", PassiveNodeKind.Small, "node"));

            Assert.AreEqual("node", index.For("notable_7", PassiveNodeKind.Notable)?.Name,
                "the row authored for the node itself was passed over");
        }

        [TestMethod]
        public void ANodeWithoutARow_WearsTheRowOfItsClass()
        {
            var index = Over(new Row(string.Empty, PassiveNodeKind.Notable, "class"));

            Assert.AreEqual("class", index.For("notable_7", PassiveNodeKind.Notable)?.Name);
        }

        /// <summary>A row addressed to one node is a point decision. Leaking it to the rest of its class
        /// would repaint a hundred nodes off one line an author wrote about one of them.</summary>
        [TestMethod]
        public void ARowForOneNode_ReachesNoOtherNodeOfThatClass()
        {
            var index = Over(new Row("notable_7", PassiveNodeKind.Notable, "node"));

            Assert.IsNull(index.For("notable_8", PassiveNodeKind.Notable));
        }

        /// <summary>The state the wheel ships in: the resource exists and says nothing. Every class has to
        /// come back empty-handed, or assigning the file would change the picture by itself.</summary>
        [TestMethod]
        public void AnEmptyLibrary_AnswersNothingForEveryClass()
        {
            var index = Over();

            foreach (PassiveNodeKind kind in Enum.GetValues<PassiveNodeKind>())
                Assert.IsNull(index.For($"{kind}_1", kind), $"an empty library answered for {kind}");
        }

        /// <summary>Two rows at one address is an authoring slip, and the answer has to be the same every
        /// time it is asked. The bottom row wins — the one the author is looking at last.</summary>
        [TestMethod]
        public void TheLastRowAtAnAddress_IsTheOneThatAnswers()
        {
            var index = Over(
                new Row("small_1", PassiveNodeKind.Small, "first"),
                new Row("small_1", PassiveNodeKind.Small, "second"),
                new Row(string.Empty, PassiveNodeKind.Keystone, "first class"),
                new Row(string.Empty, PassiveNodeKind.Keystone, "second class"));

            Assert.AreEqual("second", index.For("small_1", PassiveNodeKind.Small)?.Name);
            Assert.AreEqual("second class", index.For("keystone_1", PassiveNodeKind.Keystone)?.Name);
        }

        /// <summary>The editor puts an empty slot in the array the moment a row is added, and the wheel is
        /// drawn while the artist is still filling it in.</summary>
        [TestMethod]
        public void AnEmptySlotInTheLibrary_IsSkipped()
        {
            var index = new PassiveNodeVisualIndex<Row>(
                [null, new Row(string.Empty, PassiveNodeKind.Start, "class"), null]);

            Assert.AreEqual("class", index.For("start_1", PassiveNodeKind.Start)?.Name);
        }

        /// <summary>A node carries its own id and its class together, so the two-layer answer is one call
        /// at the drawing rather than a pair the caller has to keep in step.</summary>
        [TestMethod]
        public void AskingWithTheNodeItself_AnswersTheSameAsAskingWithItsAddress()
        {
            var index = Over(new Row("small_1", PassiveNodeKind.Small, "node"));
            var node = new PassiveNode { Id = "small_1", Kind = PassiveNodeKind.Small };

            Assert.AreEqual("node", index.For(node)?.Name);
            Assert.AreEqual("node", index.For(node.Id, node.Kind)?.Name);
        }

        /// <summary>Whitespace is not an address. The predicate is published because the drawing asks it
        /// too — a row filed as a class here and read as a node's there is exactly what pinning it
        /// prevents.</summary>
        [TestMethod]
        public void ARowWhoseIdIsBlank_IsAClassRowByTheSameTestTheDrawingAsks()
        {
            var row = new Row("   ", PassiveNodeKind.Notable, "class");

            Assert.IsFalse(IPassiveNodeVisual.IsNodeRow(row));
            Assert.AreEqual("class", Over(row).For("notable_7", PassiveNodeKind.Notable)?.Name);
        }

        private static PassiveNodeVisualIndex<Row> Over(params Row[] rows) => new(rows);

        private sealed record Row(string NodeId, PassiveNodeKind Kind, string Name) : IPassiveNodeVisual;
    }
}

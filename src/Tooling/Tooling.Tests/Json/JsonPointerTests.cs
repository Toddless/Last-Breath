namespace Tooling.Tests.Json
{
    using System;
    using Newtonsoft.Json.Linq;
    using Tooling.Json;

    /// <summary>
    /// The address of a node, as everything above it depends on being exact. A pointer that parsed one way
    /// and printed another would send an edit to a node the author never selected, and a resolve that threw
    /// on a path the file does not have would turn "not authored yet" into a crash.
    /// </summary>
    [TestClass]
    public class JsonPointerTests
    {
        private const string Text = "/npcs/3/abilities/0";
        private const string EscapedText = "/a~1b/c~0d";
        private const string SlashKey = "a/b";
        private const string TildeKey = "c~d";

        private const string Npcs = "npcs";
        private const string Abilities = "abilities";
        private const string Weird = "weird";
        private const string Missing = "nothing";

        private const string Tree = """
            {
                "npcs": [ { "id": "Ronald", "abilities": [ "Cut" ] } ],
                "weird": { "a/b": 1, "c~d": 2 },
                "": 3
            }
            """;

        private const string FirstAbility = "Cut";

        [TestMethod]
        public void Parse_EmptyText_IsTheRoot()
        {
            JsonPointer pointer = JsonPointer.Parse(string.Empty);

            Assert.IsTrue(pointer.IsRoot);
            Assert.AreEqual(JsonPointer.Root, pointer);
            Assert.AreEqual(string.Empty, pointer.ToString());
            Assert.AreEqual(0, pointer.Segments.Count);
        }

        [TestMethod]
        public void Parse_ReadsTheSegments_AndPrintsThemBack()
        {
            JsonPointer pointer = JsonPointer.Parse(Text);

            CollectionAssert.AreEqual(new[] { Npcs, "3", Abilities, "0" }, pointer.Segments.ToArray());
            Assert.AreEqual(Text, pointer.ToString());
        }

        /// <summary>A lone separator is a pointer to the key that is the empty string, not to the root — the
        /// two are different nodes and a file is allowed to hold both.</summary>
        [TestMethod]
        public void Parse_LoneSeparator_IsOneEmptySegment()
        {
            JsonPointer pointer = JsonPointer.Parse(JsonPointer.Separator.ToString());

            Assert.IsFalse(pointer.IsRoot);
            Assert.AreEqual(1, pointer.Segments.Count);
            Assert.AreEqual(string.Empty, pointer.Last);
        }

        [TestMethod]
        public void Parse_ReadsTheEscapes_AndWritesThemBack()
        {
            JsonPointer pointer = JsonPointer.Parse(EscapedText);

            Assert.AreEqual(SlashKey, pointer.Segments[0]);
            Assert.AreEqual(TildeKey, pointer.Segments[1]);
            Assert.AreEqual(EscapedText, pointer.ToString());
            Assert.AreEqual(EscapedText, JsonPointer.Root.Append(SlashKey).Append(TildeKey).ToString());
        }

        /// <summary>Bad text is refused where it is written down. A pointer that quietly took it would
        /// address some other node, and the edit would land there.</summary>
        [TestMethod]
        public void Parse_RefusesTextThatIsNotAPointer()
        {
            Assert.ThrowsException<FormatException>(() => JsonPointer.Parse(Npcs));
            Assert.ThrowsException<FormatException>(() => JsonPointer.Parse("/a~2"));
            Assert.ThrowsException<FormatException>(() => JsonPointer.Parse("/a~"));
        }

        [TestMethod]
        public void ParentAndLast_WalkOneStepBack()
        {
            JsonPointer pointer = JsonPointer.Parse(Text);

            Assert.AreEqual("0", pointer.Last);
            Assert.AreEqual("/npcs/3/abilities", pointer.Parent?.ToString());
            Assert.AreEqual("/npcs/3", pointer.Parent?.Parent?.ToString());

            Assert.IsNull(JsonPointer.Root.Parent);
            Assert.IsNull(JsonPointer.Root.Last);
        }

        [TestMethod]
        public void Append_AddsAKeyOrAnIndex()
        {
            JsonPointer pointer = JsonPointer.Root.Append(Npcs).Append(3).Append(Abilities).Append(0);

            Assert.AreEqual(Text, pointer.ToString());
        }

        [TestMethod]
        public void Resolve_FindsTheNode()
        {
            JToken root = JToken.Parse(Tree);

            Assert.AreEqual(FirstAbility, JsonPointer.Parse("/npcs/0/abilities/0").Resolve(root)?.Value<string>());
            Assert.AreEqual(1, JsonPointer.Parse("/weird/a~1b").Resolve(root)?.Value<int>());
            Assert.AreEqual(2, JsonPointer.Parse("/weird/c~0d").Resolve(root)?.Value<int>());
            Assert.AreEqual(3, JsonPointer.Parse("/").Resolve(root)?.Value<int>());
            Assert.AreSame(root, JsonPointer.Root.Resolve(root));
        }

        /// <summary>Every way of pointing at nothing answers the same way, because a tool asks about paths
        /// the author has not written yet on every keystroke of a filter box.</summary>
        [TestMethod]
        public void Resolve_APathTheTreeDoesNotHave_IsNull()
        {
            JToken root = JToken.Parse(Tree);

            Assert.IsNull(JsonPointer.Root.Append(Missing).Resolve(root));
            Assert.IsNull(JsonPointer.Parse("/npcs/7").Resolve(root), "index past the end of the array");
            Assert.IsNull(JsonPointer.Parse("/npcs/first").Resolve(root), "a key where an index is expected");
            Assert.IsNull(JsonPointer.Parse("/npcs/00").Resolve(root), "an index written with a leading zero");
            Assert.IsNull(JsonPointer.Parse("/weird/0").Resolve(root), "an index where a key is expected");
            Assert.IsNull(JsonPointer.Parse("/npcs/0/id/deeper").Resolve(root), "a step past a leaf");
        }

        [TestMethod]
        public void Pointers_AreComparedByValue()
        {
            JsonPointer parsed = JsonPointer.Parse(Text);
            JsonPointer built = JsonPointer.Root.Append(Npcs).Append(3).Append(Abilities).Append(0);

            Assert.AreEqual(parsed, built);
            Assert.IsTrue(parsed == built);
            Assert.AreEqual(parsed.GetHashCode(), built.GetHashCode());

            Assert.AreNotEqual(parsed, built.Parent);
            Assert.IsTrue(parsed != built.Parent);
            Assert.IsFalse(parsed.Equals(null));
        }

        /// <summary>The escape is part of the value, not of the address: two different keys must not collapse
        /// into one pointer.</summary>
        [TestMethod]
        public void Pointers_KeepEscapedKeysApart()
        {
            Assert.AreNotEqual(JsonPointer.Root.Append(SlashKey), JsonPointer.Root.Append("a").Append("b"));
        }
    }
}

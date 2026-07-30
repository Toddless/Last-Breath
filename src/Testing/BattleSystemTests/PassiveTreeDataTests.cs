namespace LastBreathTest.BattleSystemTests
{
    using System.Text;
    using Core.Data.GameData;
    using Core.Enums;
    using Core.PassiveTree;

    /// <summary>
    /// The tree is authored by hand in one file and read by both the game and the editor, so the two
    /// things that can silently rot are covered here: the writer must reproduce the shipped file byte
    /// for byte (otherwise the next content diff is unreadable), and a typo must cost its own record
    /// instead of quietly becoming the first enum member.
    /// </summary>
    [TestClass]
    public class PassiveTreeDataTests
    {
        private static string SharedDataRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null)
            {
                string candidate = Path.Combine(directory.FullName, "Data", "Shared");
                if (Directory.Exists(candidate)) return candidate;
                directory = directory.Parent;
            }

            throw new InvalidOperationException($"Data/Shared symlink not found above {AppContext.BaseDirectory}");
        }

        private static string ShippedTreePath() =>
            Path.Combine(SharedDataRoot(), DataCatalog.PassiveTree, PassiveTreeFormat.DefaultFileName);

        private static int FirstDifference(byte[] left, byte[] right)
        {
            int shared = Math.Min(left.Length, right.Length);
            for (int index = 0; index < shared; index++)
                if (left[index] != right[index])
                    return index;

            return left.Length == right.Length ? -1 : shared;
        }

        [TestMethod]
        public void RoundTripOfTheShippedTreeIsByteIdentical()
        {
            byte[] original = File.ReadAllBytes(ShippedTreePath());
            List<string> issues = [];

            PassiveTreeDocument document = PassiveTreeSerializer.Deserialize(Encoding.UTF8.GetString(original), issues);
            byte[] rewritten = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
                .GetBytes(PassiveTreeSerializer.Serialize(document));

            Assert.AreEqual(0, issues.Count, string.Join("; ", issues));
            Assert.AreEqual(-1, FirstDifference(original, rewritten),
                $"load+save changed the file ({original.Length} bytes in, {rewritten.Length} out)");
        }

        [TestMethod]
        public void ShippedTreeLoadsThroughTheDataPipelineWithoutIssues()
        {
            var provider = new PassiveTreeProvider();
            var service = new GameDataService(new FileSystemDataSource(SharedDataRoot()), [provider]);
            List<string> failures = [];
            service.LoadFailed += (context, exception) => failures.Add($"{context}: {exception.Message}");

            service.LoadAll();

            CollectionAssert.AreEqual(new[] { DataCatalog.PassiveTree }, provider.Catalogs.ToArray());
            Assert.AreEqual(0, failures.Count, string.Join("; ", failures));
            Assert.AreEqual(0, provider.Issues.Count, string.Join("; ", provider.Issues));
            Assert.IsTrue(provider.Tree.Nodes.Count > 0, "the catalog produced an empty tree");
            Assert.IsTrue(provider.Tree.Links.Count > 0, "the catalog produced a tree without edges");
            Assert.IsTrue(provider.Tree.Budget > 0, "the catalog produced a tree without a point budget");
        }

        [TestMethod]
        public void MisspelledNodeKindSkipsTheNodeInsteadOfDefaultingToTheFirstMember()
        {
            const string json = """
                {
                    "version": 1,
                    "budget": 10,
                    "nodes": [
                        { "id": "sound", "kind": "Notable", "x": 0.0, "y": 0.0 },
                        { "id": "typo", "kind": "Smal", "x": 1.0, "y": 1.0 }
                    ],
                    "edges": []
                }
                """;
            List<string> issues = [];

            PassiveTreeDocument document = PassiveTreeSerializer.Deserialize(json, issues);

            Assert.IsNull(document.Find("typo"), "a misspelled kind silently became a Small node");
            Assert.IsNotNull(document.Find("sound"));
            Assert.AreEqual(1, issues.Count, string.Join("; ", issues));
            StringAssert.Contains(issues[0], "typo");
        }

        [TestMethod]
        public void MisspelledModifierParameterCostsItsLineAndNotTheNode()
        {
            const string json = """
                {
                    "version": 1,
                    "budget": 10,
                    "nodes": [
                        {
                            "id": "notable_1",
                            "kind": "Notable",
                            "x": 0.0,
                            "y": 0.0,
                            "modifiers": [
                                { "parameter": "Armour", "valueType": "Increase", "value": 0.06 },
                                { "parameter": "Armor", "valueType": "Increase", "value": 0.06 }
                            ]
                        }
                    ],
                    "edges": []
                }
                """;
            List<string> issues = [];

            PassiveTreeDocument document = PassiveTreeSerializer.Deserialize(json, issues);
            PassiveNode? node = document.Find("notable_1");

            Assert.IsNotNull(node);
            Assert.AreEqual(1, node.Modifiers.Count);
            Assert.AreEqual(EntityParameter.Armor, node.Modifiers[0].Parameter);
            Assert.AreEqual(1, issues.Count, string.Join("; ", issues));
        }

        [TestMethod]
        public void FreeTextConditionSurvivesSaveAndLoad()
        {
            var document = new PassiveTreeDocument { Budget = 65 };
            var node = new PassiveNode { Id = "small_1", Kind = PassiveNodeKind.Small, Stance = Stance.Strength };
            node.Modifiers.Add(new ModifierLine
            {
                Parameter = EntityParameter.PhysicalDamage,
                ValueType = ModifierValueType.Increase,
                Value = 0.08f,
                Condition = "while below half health"
            });
            document.AddNode(node);
            List<string> issues = [];

            PassiveTreeDocument reloaded = PassiveTreeSerializer.Deserialize(PassiveTreeSerializer.Serialize(document), issues);
            ModifierLine? line = reloaded.Find("small_1")?.Modifiers[0];

            Assert.AreEqual(0, issues.Count, string.Join("; ", issues));
            Assert.IsNotNull(line);
            Assert.AreEqual("while below half health", line.Condition);
            Assert.IsTrue(line.IsConditional);
            Assert.AreEqual(65, reloaded.Budget);
        }

        [TestMethod]
        public void ProviderReportsBrokenRecordsAndStillPublishesTheRestOfTheTree()
        {
            const string json = """
                {
                    "version": 1,
                    "budget": 12,
                    "nodes": [
                        { "id": "start_str", "kind": "Start", "stance": "Strength", "x": 0.0, "y": 0.0, "abilityId": "Ability_X" },
                        { "id": "broken", "kind": "Keystone", "stance": "Stength", "x": 5.0, "y": 5.0 }
                    ],
                    "edges": [ { "from": "start_str", "to": "broken" } ]
                }
                """;
            var provider = new PassiveTreeProvider();

            provider.Apply(DataCatalog.PassiveTree, new GameDataFile(PassiveTreeFormat.DefaultFileName, json));

            Assert.AreEqual(1, provider.Tree.Nodes.Count);
            Assert.IsTrue(provider.Tree.Contains("start_str"));
            // The bad node and the edge that leaned on it: reported, skipped, load still stands.
            Assert.AreEqual(2, provider.Issues.Count, string.Join("; ", provider.Issues));
        }

        [TestMethod]
        public void UnreadableFileIsReportedInsteadOfThrowing()
        {
            List<string> issues = [];

            PassiveTreeDocument document = PassiveTreeSerializer.Deserialize("{ not json", issues);

            Assert.AreEqual(0, document.Nodes.Count);
            Assert.AreEqual(1, issues.Count);
        }
    }
}

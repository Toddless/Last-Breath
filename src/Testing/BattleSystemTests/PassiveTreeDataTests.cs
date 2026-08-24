namespace LastBreathTest.BattleSystemTests
{
    using System.Text;
    using Core.Data.GameData;
    using Core.Enums;
    using Core.Modifiers.Conditions;
    using Core.Modifiers.Context;
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
        private static string ShippedTreePath() =>
            Path.Combine(SharedData.Catalog(DataCatalog.PassiveTree), PassiveTreeFormat.DefaultFileName);

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
            var service = new GameDataService(new FileSystemDataSource(SharedData.Root()), [provider]);
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
        public void TheShippedTreeLeavesNoNodeNothingLinksTo()
        {
            // Reachability is the only way a node is ever bought, so an unlinked one is content the
            // player cannot get to at any budget — and a shape the wheel draws floating on its own.
            List<string> issues = [];
            PassiveTreeDocument tree = PassiveTreeSerializer.Load(ShippedTreePath(), issues);

            PassiveNode[] purchasable = [.. tree.Nodes.Where(node => NodeKindRules.CostsPoint(node.Kind))];
            string[] unlinked = [.. purchasable.Where(node => tree.Neighbours(node.Id).Count == 0).Select(node => node.Id)];

            Assert.AreEqual(0, issues.Count, string.Join("; ", issues));
            // An absence proves nothing about a document that holds nothing: a tree that failed to load
            // would answer "no unlinked nodes" the same way a clean one does.
            Assert.IsTrue(purchasable.Length > 0, "the shipped tree produced no node to check");
            Assert.AreEqual(0, unlinked.Length, "the shipped tree carries nodes nothing links to: " + string.Join(", ", unlinked));
        }

        [TestMethod]
        public void EveryConditionTheShippedTreeNamesIsInTheShippedCatalog()
        {
            // A condition is a reference across two hand-written files, and the reader of the tree cannot
            // check it: the catalog is only consulted when the allocation turns into a contribution. An id
            // nobody defined therefore costs the line it guards — silently, at the far end of the game —
            // and the draft this tree grew out of wrote its conditions as English sentences.
            var tree = new PassiveTreeProvider();
            var catalog = new ConditionProvider(ConditionParser.Default());
            new GameDataService(new FileSystemDataSource(SharedData.Root()), [tree, catalog]).LoadAll();

            string[] named = [.. tree.Tree.Nodes
                .SelectMany(node => node.Modifiers.Select(line => line.Condition)
                    .Concat(node.ContextModifiers.Select(line => line.Condition)))
                .Where(condition => condition.Length > 0)
                .Distinct(StringComparer.Ordinal)];

            foreach (string condition in named)
                Assert.IsTrue(catalog.TryResolve(condition, out _),
                    $"the shipped tree gates a line on '{condition}', which the Conditions catalog does not hold — the line is dropped wherever it is read");
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

        /// <summary>The spread is a decision about the layout, so it travels in the document the layout
        /// travels in — the authoring tool writes it and the game reads it, and neither may hold a spread
        /// the other cannot see.</summary>
        [TestMethod]
        public void TheAuthoredSpreadSurvivesSaveAndLoad()
        {
            const float authored = 2.25f;
            var document = new PassiveTreeDocument { Budget = 30, Spread = authored };
            document.AddNode(new PassiveNode { Id = "small_1", Kind = PassiveNodeKind.Small, Stance = Stance.Strength });
            List<string> issues = [];

            string json = PassiveTreeSerializer.Serialize(document);
            PassiveTreeDocument reloaded = PassiveTreeSerializer.Deserialize(json, issues);

            Assert.AreEqual(0, issues.Count, string.Join("; ", issues));
            StringAssert.Contains(json, "\"spread\": 2.25", "the spread never reached the file");
            Assert.AreEqual(authored, reloaded.Spread, 0.0001f);
        }

        /// <summary>Every tree written before the field existed, and every tree laid out at the authored
        /// default: no key, and the default is exactly what those files meant. The writer leaves it out
        /// at that value for the same reason — a line saying "unchanged" would re-diff every tree.</summary>
        [TestMethod]
        public void ADocumentWithNoSpreadFieldReadsAsTheDefault()
        {
            const string json = """
                {
                    "version": 1,
                    "budget": 10,
                    "nodes": [ { "id": "small_1", "kind": "Small", "x": 0.0, "y": 0.0 } ],
                    "edges": []
                }
                """;
            List<string> issues = [];

            PassiveTreeDocument document = PassiveTreeSerializer.Deserialize(json, issues);

            Assert.AreEqual(0, issues.Count, string.Join("; ", issues));
            Assert.AreEqual(Core.PassiveTree.View.CanvasTransform.DefaultSpread, document.Spread, 0.0001f);
            Assert.IsFalse(PassiveTreeSerializer.Serialize(document).Contains("spread"),
                "the default spread was written back into a file that never carried it");
        }

        /// <summary>A spread that is not a spread at all falls back to the default rather than clamping
        /// up to the nearest legal stop, which would be a layout nobody composed. Anything else lands on
        /// the grid the view snaps to, so the file and the picture cannot part company.</summary>
        [TestMethod]
        public void ASpreadOffTheGridIsSnapped_AndAnImpossibleOneFallsBackToTheDefault()
        {
            List<string> issues = [];

            Assert.AreEqual(2.25f, PassiveTreeSerializer.Deserialize(TreeWithSpread("2.3"), issues).Spread, 0.0001f);
            Assert.AreEqual(Core.PassiveTree.View.CanvasTransform.MaxSpread,
                PassiveTreeSerializer.Deserialize(TreeWithSpread("400"), issues).Spread, 0.0001f);
            Assert.AreEqual(Core.PassiveTree.View.CanvasTransform.DefaultSpread,
                PassiveTreeSerializer.Deserialize(TreeWithSpread("0"), issues).Spread, 0.0001f);

            Assert.AreEqual(0, issues.Count, string.Join("; ", issues));
        }

        private static string TreeWithSpread(string spread) => $$"""
            {
                "version": 1,
                "budget": 10,
                "spread": {{spread}},
                "nodes": [ { "id": "small_1", "kind": "Small", "x": 0.0, "y": 0.0 } ],
                "edges": []
            }
            """;

        [TestMethod]
        public void ContextLineSurvivesSaveAndLoadOnItsOwnChannel()
        {
            var document = new PassiveTreeDocument { Budget = 20 };
            var node = new PassiveNode { Id = "notable_1", Kind = PassiveNodeKind.Notable };
            node.Modifiers.Add(new ModifierLine { Parameter = EntityParameter.Armor, ValueType = ModifierValueType.Increase, Value = 0.06f });
            node.ContextModifiers.Add(new ContextModifierLine
            {
                Parameter = ContextParameter.HealingEfficiency,
                ValueType = ModifierValueType.Increase,
                Value = 0.15f,
                Condition = "while below half health"
            });
            document.AddNode(node);
            List<string> issues = [];

            PassiveNode? reloaded = PassiveTreeSerializer
                .Deserialize(PassiveTreeSerializer.Serialize(document), issues)
                .Find("notable_1");

            Assert.AreEqual(0, issues.Count, string.Join("; ", issues));
            Assert.IsNotNull(reloaded);
            Assert.AreEqual(1, reloaded.Modifiers.Count, "the parametric channel did not survive on its own");
            Assert.AreEqual(1, reloaded.ContextModifiers.Count);
            Assert.AreEqual(ContextParameter.HealingEfficiency, reloaded.ContextModifiers[0].Parameter);
            Assert.AreEqual(ModifierValueType.Increase, reloaded.ContextModifiers[0].ValueType);
            Assert.AreEqual(0.15f, reloaded.ContextModifiers[0].Value, 0.0001f);
            Assert.AreEqual("while below half health", reloaded.ContextModifiers[0].Condition);
            Assert.AreEqual(2, reloaded.LineCount);
        }

        [TestMethod]
        public void ContextLinesAreWrittenInTheCanonicalShape()
        {
            // The writer emits LF whatever the host does, so the expectation is normalized to match.
            string canonical = """
                {
                    "version": 1,
                    "budget": 20,
                    "nodes": [
                        {
                            "id": "notable_1",
                            "kind": "Notable",
                            "x": 0.0,
                            "y": 0.0,
                            "modifiers": [
                                {
                                    "parameter": "Armor",
                                    "valueType": "Increase",
                                    "value": 0.06
                                }
                            ],
                            "contextModifiers": [
                                {
                                    "parameter": "HealingEfficiency",
                                    "valueType": "Increase",
                                    "value": 0.15
                                },
                                {
                                    "parameter": "AttacksIgnoreResistances",
                                    "valueType": "Flag"
                                }
                            ]
                        }
                    ],
                    "edges": []
                }

                """.ReplaceLineEndings("\n");
            List<string> issues = [];

            string rewritten = PassiveTreeSerializer.Serialize(PassiveTreeSerializer.Deserialize(canonical, issues));

            Assert.AreEqual(0, issues.Count, string.Join("; ", issues));
            Assert.AreEqual(canonical, rewritten, "load+save changed a file carrying context lines");
        }

        [TestMethod]
        public void FlagOnAParametricLineIsReportedAndCostsOnlyThatLine()
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
                                { "parameter": "Armor", "valueType": "flag" },
                                { "parameter": "Armor", "valueType": "Increase", "value": 0.06 }
                            ]
                        }
                    ],
                    "edges": []
                }
                """;
            List<string> issues = [];

            PassiveNode? node = PassiveTreeSerializer.Deserialize(json, issues).Find("notable_1");

            Assert.IsNotNull(node);
            Assert.AreEqual(1, node.Modifiers.Count, "a flag became a parameter modifier nothing can read");
            Assert.AreEqual(ModifierValueType.Increase, node.Modifiers[0].ValueType);
            Assert.AreEqual(1, issues.Count, string.Join("; ", issues));
        }

        [TestMethod]
        public void FlagContextLineCarriesNoAuthoredNumber()
        {
            const string json = """
                {
                    "version": 1,
                    "budget": 10,
                    "nodes": [
                        {
                            "id": "keystone_1",
                            "kind": "Keystone",
                            "x": 0.0,
                            "y": 0.0,
                            "contextModifiers": [
                                { "parameter": "AttacksIgnoreResistances", "valueType": "flag", "value": 0.42 }
                            ]
                        }
                    ],
                    "edges": []
                }
                """;
            List<string> issues = [];

            PassiveTreeDocument document = PassiveTreeSerializer.Deserialize(json, issues);
            ContextModifierLine? line = document.Find("keystone_1")?.ContextModifiers[0];

            Assert.AreEqual(0, issues.Count, string.Join("; ", issues));
            Assert.IsNotNull(line);
            Assert.IsTrue(line.IsFlag);
            Assert.AreEqual(ContextModifierLine.FlagValue, line.Value, 0.0001f, "a switch was read as a quantity");
            StringAssert.Contains(PassiveTreeSerializer.Serialize(document), "\"valueType\": \"Flag\"");
            Assert.IsFalse(PassiveTreeSerializer.Serialize(document).Contains("0.42"), "a flag was written back with a number to edit");
        }

        [TestMethod]
        public void FlagOnAKnobThatCarriesAValueIsReportedAndCostsOnlyThatLine()
        {
            // Healing efficiency reads a number. Written as a switch it stops being "+15% healing" and
            // becomes the pinned 1 — a keystone handing out +100% healing that nobody typed, and nothing
            // notices until the line reaches the heal pipeline.
            const string json = """
                {
                    "version": 1,
                    "budget": 10,
                    "nodes": [
                        {
                            "id": "keystone_1",
                            "kind": "Keystone",
                            "x": 0.0,
                            "y": 0.0,
                            "contextModifiers": [
                                { "parameter": "HealingEfficiency", "valueType": "flag" },
                                { "parameter": "HealingEfficiency", "valueType": "Increase", "value": 0.15 }
                            ]
                        }
                    ],
                    "edges": []
                }
                """;
            List<string> issues = [];

            PassiveNode? node = PassiveTreeSerializer.Deserialize(json, issues).Find("keystone_1");

            Assert.IsNotNull(node);
            Assert.AreEqual(1, node.ContextModifiers.Count, "a switch was accepted on a knob that reads a value");
            Assert.AreEqual(ModifierValueType.Increase, node.ContextModifiers[0].ValueType);
            Assert.AreEqual(0.15f, node.ContextModifiers[0].Value, 0.0001f);
            Assert.AreEqual(1, issues.Count, string.Join("; ", issues));
        }

        [TestMethod]
        public void NumberOnASwitchKnobIsReportedAndCostsOnlyThatLine()
        {
            // The other half of the same rule: a switch has no reader, so an amount authored on it is a
            // number no pipeline will ever look at — the node would promise a magnitude it cannot deliver.
            const string json = """
                {
                    "version": 1,
                    "budget": 10,
                    "nodes": [
                        {
                            "id": "keystone_1",
                            "kind": "Keystone",
                            "x": 0.0,
                            "y": 0.0,
                            "contextModifiers": [
                                { "parameter": "AttacksIgnoreResistances", "valueType": "Increase", "value": 0.35 },
                                { "parameter": "AttacksIgnoreResistances", "valueType": "flag" }
                            ]
                        }
                    ],
                    "edges": []
                }
                """;
            List<string> issues = [];

            PassiveNode? node = PassiveTreeSerializer.Deserialize(json, issues).Find("keystone_1");

            Assert.IsNotNull(node);
            Assert.AreEqual(1, node.ContextModifiers.Count, "an amount was accepted on a knob that reads nothing");
            Assert.AreEqual(ModifierValueType.Flag, node.ContextModifiers[0].ValueType);
            Assert.AreEqual(1, issues.Count, string.Join("; ", issues));
        }

        [TestMethod]
        public void ContextLineNamingSomethingOutsideTheEnumIsReportedAndSkipped()
        {
            // A bare number parses into every enum, so "999" would become a member that does not exist —
            // and a context knob that does not exist has no binding to throw in battle later.
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
                            "contextModifiers": [
                                { "parameter": "999", "valueType": "Increase", "value": 0.15 },
                                { "parameter": "BleedDamage", "valueType": "Increase", "value": 0.35 }
                            ]
                        }
                    ],
                    "edges": []
                }
                """;
            List<string> issues = [];

            PassiveNode? node = PassiveTreeSerializer.Deserialize(json, issues).Find("notable_1");

            Assert.IsNotNull(node);
            Assert.AreEqual(1, node.ContextModifiers.Count);
            Assert.AreEqual(ContextParameter.BleedDamage, node.ContextModifiers[0].Parameter);
            Assert.AreEqual(1, issues.Count, string.Join("; ", issues));
        }

        [TestMethod]
        public void EveryContextKnobIsAuthorableBecauseEveryKnobIsWired()
        {
            // The reader refuses a knob that reaches no pipeline. While every member is wired that gate
            // costs nothing; the day a member ships without a binding, the file loses the line instead of
            // the battle losing the fight.
            CollectionAssert.AreEquivalent(Enum.GetValues<ContextParameter>(), ContextKnobs.Bound.ToArray());
        }

        /// <summary>Unstamped records are lines of their own, so three of them across the two channels are
        /// three lines whatever road each takes.</summary>
        [TestMethod]
        public void BothLineChannelsCountAgainstTheSameNodeBudget()
        {
            var document = new PassiveTreeDocument();
            var node = new PassiveNode { Id = "small_1", Kind = PassiveNodeKind.Small, Stance = Stance.Strength };
            node.Modifiers.Add(new ModifierLine { Parameter = EntityParameter.Armor, ValueType = ModifierValueType.Increase, Value = 0.06f });
            node.Modifiers.Add(new ModifierLine { Parameter = EntityParameter.Evade, ValueType = ModifierValueType.Increase, Value = 0.06f });
            node.ContextModifiers.Add(new ContextModifierLine { Parameter = ContextParameter.BleedDamage, Value = 0.1f });
            document.AddNode(node);

            List<string> issues = document.Validate();

            Assert.AreEqual(3, node.LineCount, "records nobody joined into a composite stopped costing a slot each");
            Assert.IsTrue(issues.Any(issue => issue.Contains("small_1") && issue.Contains("at most 2")),
                "a payload past the limit slipped through on the context channel: " + string.Join("; ", issues));
        }

        /// <summary>The composite stamp is authored by hand and read by both the game and the tool, so it
        /// has to survive a save; an unstamped line must come back unstamped rather than as a blank group
        /// that would silently swallow its neighbour.</summary>
        [TestMethod]
        public void ACompositeStampSurvivesTheRoundTripAndAnUnstampedLineComesBackWithout()
        {
            var document = new PassiveTreeDocument { Budget = 20 };
            var node = new PassiveNode { Id = "notable_1", Kind = PassiveNodeKind.Notable };
            node.Modifiers.Add(new ModifierLine
            {
                Parameter = EntityParameter.Mana, ValueType = ModifierValueType.Increase, Value = 0.25f, GroupId = "mana"
            });
            node.Modifiers.Add(new ModifierLine { Parameter = EntityParameter.Armor, ValueType = ModifierValueType.Increase, Value = 0.06f });
            node.ContextModifiers.Add(new ContextModifierLine
            {
                Parameter = ContextParameter.BleedDamage, ValueType = ModifierValueType.Increase, Value = 0.1f, GroupId = "mana"
            });
            document.AddNode(node);
            List<string> issues = [];

            PassiveNode? reloaded = PassiveTreeSerializer
                .Deserialize(PassiveTreeSerializer.Serialize(document), issues)
                .Find("notable_1");

            Assert.AreEqual(0, issues.Count, string.Join("; ", issues));
            Assert.IsNotNull(reloaded);
            Assert.AreEqual("mana", reloaded.Modifiers[0].GroupId);
            Assert.IsNull(reloaded.Modifiers[1].GroupId, "a line nobody stamped came back inside a composite");
            Assert.AreEqual("mana", reloaded.ContextModifiers[0].GroupId, "the stamp did not survive on the context channel");

            // A blank stamp is not a stamp: left as written it would be a group id like any other, joining
            // every other blank-stamped record of the node into one sentence and going back into the file.
            const string blank = """
                {
                    "version": 1,
                    "nodes": [
                        {
                            "id": "small_1",
                            "kind": "Small",
                            "x": 0.0,
                            "y": 0.0,
                            "modifiers": [ { "parameter": "Armor", "valueType": "Increase", "value": 0.06, "groupId": "" } ]
                        }
                    ],
                    "edges": []
                }
                """;
            List<string> blankIssues = [];
            PassiveTreeDocument blankDocument = PassiveTreeSerializer.Deserialize(blank, blankIssues);

            Assert.AreEqual(0, blankIssues.Count, string.Join("; ", blankIssues));
            Assert.IsNull(blankDocument.Find("small_1")!.Modifiers[0].GroupId, "a blank stamp came back as a composite of one");
            Assert.IsFalse(PassiveTreeSerializer.Serialize(blankDocument).Contains("groupId"), "a blank stamp was written back into the file");
        }

        /// <summary>What a composite costs: the records sharing a stamp are one line, the record beside
        /// them is another — two lines out of three records.</summary>
        [TestMethod]
        public void RecordsOfOneCompositeCostTheNodeASingleLine()
        {
            var node = new PassiveNode { Id = "small_1", Kind = PassiveNodeKind.Small };
            node.Modifiers.Add(new ModifierLine
            {
                Parameter = EntityParameter.Mana, ValueType = ModifierValueType.Increase, Value = 0.02f, GroupId = "mana"
            });
            node.Modifiers.Add(new ModifierLine
            {
                Parameter = EntityParameter.ManaRecovery, ValueType = ModifierValueType.Increase, Value = 0.03f, GroupId = "mana"
            });
            node.Modifiers.Add(new ModifierLine { Parameter = EntityParameter.Armor, ValueType = ModifierValueType.Increase, Value = 0.06f });

            Assert.AreEqual(2, node.LineCount, "a composite was charged per record instead of per line");
        }

        /// <summary>A composite may be spelled by records taking different roads to the fighter, and the
        /// player still reads one line — so the budget is charged once.</summary>
        [TestMethod]
        public void ACompositeSpanningBothChannelsIsOneLineOfTheBudget()
        {
            var node = new PassiveNode { Id = "notable_1", Kind = PassiveNodeKind.Notable };
            node.Modifiers.Add(new ModifierLine
            {
                Parameter = EntityParameter.Mana, ValueType = ModifierValueType.Increase, Value = 0.25f, GroupId = "mana"
            });
            node.ContextModifiers.Add(new ContextModifierLine
            {
                Parameter = ContextParameter.ManaOnHit, ValueType = ModifierValueType.Flat, Value = 25f, GroupId = "mana"
            });

            Assert.AreEqual(1, node.LineCount, "one line spelled through both channels was charged twice");
        }

        /// <summary>One sentence hangs on one gate. Records of a composite naming different conditions
        /// would print as half-held-up, so the rule is reported rather than guessed at print time.</summary>
        [TestMethod]
        public void ACompositeWhoseRecordsNameDifferentGatesIsReported()
        {
            var document = new PassiveTreeDocument();
            var node = new PassiveNode { Id = "small_1", Kind = PassiveNodeKind.Small, Stance = Stance.Strength };
            node.Modifiers.Add(new ModifierLine
            {
                Parameter = EntityParameter.Mana,
                ValueType = ModifierValueType.Increase,
                Value = 0.02f,
                GroupId = "mana",
                Condition = "while wounded"
            });
            node.Modifiers.Add(new ModifierLine
            {
                Parameter = EntityParameter.ManaRecovery, ValueType = ModifierValueType.Increase, Value = 0.03f, GroupId = "mana"
            });
            document.AddNode(node);

            List<string> issues = document.Validate();

            Assert.IsTrue(issues.Any(issue => issue.Contains("small_1") && issue.Contains("more than one condition")),
                "a line half held up by a gate was accepted: " + string.Join("; ", issues));
        }

        /// <summary>The same rule where the halves take different roads: the gate a context record names is
        /// as much the line's gate as a parametric one, and the sentence is printed off the leading record,
        /// so a mismatch on the other channel would silently promise an always-on bonus.</summary>
        [TestMethod]
        public void ACompositeGatedOnOneChannelOnlyIsReported()
        {
            var document = new PassiveTreeDocument();
            var node = new PassiveNode { Id = "notable_1", Kind = PassiveNodeKind.Notable, Stance = Stance.Strength };
            node.Modifiers.Add(new ModifierLine
            {
                Parameter = EntityParameter.Mana, ValueType = ModifierValueType.Increase, Value = 0.25f, GroupId = "mana"
            });
            node.ContextModifiers.Add(new ContextModifierLine
            {
                Parameter = ContextParameter.ManaOnHit,
                ValueType = ModifierValueType.Flat,
                Value = 25f,
                GroupId = "mana",
                Condition = "while wounded"
            });
            document.AddNode(node);

            List<string> issues = document.Validate();

            Assert.IsTrue(issues.Any(issue => issue.Contains("notable_1") && issue.Contains("more than one condition")),
                "a gate named on the context channel alone was accepted: " + string.Join("; ", issues));
        }

        /// <summary>A value measured off a carrier is a rate, and a rate inside a list of outright bonuses
        /// reads as one of them — so it keeps a line to itself.</summary>
        [TestMethod]
        public void APerUnitValueInsideACompositeIsReported()
        {
            var document = new PassiveTreeDocument();
            var node = new PassiveNode { Id = "small_1", Kind = PassiveNodeKind.Small, Stance = Stance.Strength };
            node.Modifiers.Add(new ModifierLine
            {
                Parameter = EntityParameter.Mana, ValueType = ModifierValueType.Increase, Value = 0.02f, GroupId = "mana"
            });
            node.Modifiers.Add(new ModifierLine
            {
                Parameter = EntityParameter.ManaRecovery,
                ValueType = ModifierValueType.Increase,
                Value = 0.01f,
                PerParameter = EntityParameter.Intelligence,
                GroupId = "mana"
            });
            document.AddNode(node);

            List<string> issues = document.Validate();

            Assert.IsTrue(issues.Any(issue => issue.Contains("small_1") && issue.Contains("per-unit")),
                "a rate was folded into a composite sentence: " + string.Join("; ", issues));
        }

        [TestMethod]
        public void ASmallNodeCarriesTwoLinesWithoutBeingCalledOverfull()
        {
            var document = new PassiveTreeDocument();
            var node = new PassiveNode { Id = "small_1", Kind = PassiveNodeKind.Small, Stance = Stance.Strength };
            node.Modifiers.Add(new ModifierLine { Parameter = EntityParameter.Armor, ValueType = ModifierValueType.Increase, Value = 0.06f });
            node.Modifiers.Add(new ModifierLine { Parameter = EntityParameter.Evade, ValueType = ModifierValueType.Increase, Value = 0.06f });
            document.AddNode(node);

            List<string> issues = document.Validate();

            Assert.IsFalse(issues.Any(issue => issue.Contains("small_1") && issue.Contains("at most")),
                "a small node was refused its second line: " + string.Join("; ", issues));
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

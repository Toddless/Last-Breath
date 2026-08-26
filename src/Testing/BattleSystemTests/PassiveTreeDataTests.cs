namespace LastBreathTest.BattleSystemTests
{
    using System.Text;
    using Core.Data.GameData;
    using Core.Enums;
    using Core.Modifiers.Conditions;
    using Core.Modifiers.Context;
    using Core.PassiveTree;
    using Core.PassiveTree.Summary;
    using Core.PassiveTree.View;

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

        /// <summary>A file that names no budget is read at the figure the design settled on, not at
        /// whatever the last edit of the constant happened to leave behind: the number decides how much
        /// tree a character ever owns, so it is pinned here rather than inferred from the shipped file —
        /// which states its own budget and would hide a drift in the fallback.</summary>
        [TestMethod]
        public void ADocumentWithNoBudgetFieldReadsAsTheDesignBudget()
        {
            const string json = """
                {
                    "version": 1,
                    "nodes": [ { "id": "small_1", "kind": "Small", "x": 0.0, "y": 0.0 } ],
                    "edges": []
                }
                """;
            List<string> issues = [];

            PassiveTreeDocument document = PassiveTreeSerializer.Deserialize(json, issues);

            Assert.AreEqual(0, issues.Count, string.Join("; ", issues));
            Assert.AreEqual(65, PassiveTreeDocument.DefaultBudget, "the design budget moved away from the 65 the tree is authored against");
            Assert.AreEqual(65, document.Budget, "a file naming no budget was read at some other figure");
            Assert.AreEqual(65, new PassiveTreeDocument().Budget, "a tree built in the tool starts at a budget nobody designed");
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

        /// <summary>A node that hands over a passive instead of lines: the id it names and the numbers the
        /// passive is tuned by both survive the round trip, and the numbers keep the order they were
        /// authored in — sorting them would re-diff a file nobody edited.</summary>
        [TestMethod]
        public void APassiveNodeCarriesItsIdAndItsNumbersThroughTheRoundTrip()
        {
            string canonical = """
                {
                    "version": 1,
                    "budget": 20,
                    "nodes": [
                        {
                            "id": "keystone_1",
                            "kind": "Keystone",
                            "x": 0.0,
                            "y": 0.0,
                            "title": "Porcupine",
                            "passiveId": "Passive_Skill_Porcupine",
                            "properties": {
                                "damagePercent": 0.25,
                                "armorPercent": 0.1
                            }
                        }
                    ],
                    "edges": []
                }

                """.ReplaceLineEndings("\n");
            List<string> issues = [];

            PassiveTreeDocument document = PassiveTreeSerializer.Deserialize(canonical, issues);
            PassiveNode? node = document.Find("keystone_1");

            Assert.AreEqual(0, issues.Count, string.Join("; ", issues));
            Assert.IsNotNull(node);
            Assert.AreEqual("Passive_Skill_Porcupine", node.PassiveId);
            Assert.IsTrue(node.IsPassive);
            Assert.IsFalse(node.HasLines);
            Assert.AreEqual(2, node.Properties.Count, "the numbers the passive is built with were lost at the door");
            Assert.AreEqual(0.25f, node.Properties["damagePercent"], 0.0001f);
            Assert.AreEqual(0.1f, node.Properties["armorPercent"], 0.0001f);
            Assert.AreEqual(canonical, PassiveTreeSerializer.Serialize(document),
                "load+save changed a file carrying a passive node");
        }

        /// <summary>Every tree written before the keys existed: no id, no numbers, and the writer puts
        /// neither back — a node that never granted a passive must not start claiming one.</summary>
        [TestMethod]
        public void ANodeWithoutTheNewKeysReadsAndIsWrittenExactlyAsBefore()
        {
            const string json = """
                {
                    "version": 1,
                    "budget": 10,
                    "nodes": [
                        {
                            "id": "small_1",
                            "kind": "Small",
                            "x": 0.0,
                            "y": 0.0,
                            "modifiers": [ { "parameter": "Armor", "valueType": "Increase", "value": 0.06 } ]
                        }
                    ],
                    "edges": []
                }
                """;
            List<string> issues = [];

            PassiveTreeDocument document = PassiveTreeSerializer.Deserialize(json, issues);
            PassiveNode? node = document.Find("small_1");
            string rewritten = PassiveTreeSerializer.Serialize(document);

            Assert.AreEqual(0, issues.Count, string.Join("; ", issues));
            Assert.IsNotNull(node);
            Assert.IsNull(node.PassiveId);
            Assert.IsFalse(node.IsPassive);
            Assert.AreEqual(0, node.Properties.Count);
            Assert.AreEqual(1, node.Modifiers.Count);
            Assert.IsFalse(rewritten.Contains("passiveId"), "a node that grants no passive was written back claiming one");
            Assert.IsFalse(rewritten.Contains("properties"), "empty numbers were written into a file that never carried them");
        }

        /// <summary>Two payloads for one point: no reader can say which one the node owes, so it is refused
        /// whole. The rule is "not both", not "a keystone must be a passive" — the keystone beside it says
        /// its piece in lines and is untouched, which is how every keystone reads until it is converted.</summary>
        [TestMethod]
        public void ANodeCarryingAPassiveAndLinesAtOnceIsRefusedWholeAndItsNeighbourStands()
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
                            "passiveId": "Passive_Skill_Execute",
                            "properties": { "threshold": 0.2 },
                            "modifiers": [ { "parameter": "Armor", "valueType": "Increase", "value": 0.06 } ]
                        },
                        {
                            "id": "keystone_2",
                            "kind": "Keystone",
                            "x": 1.0,
                            "y": 1.0,
                            "contextModifiers": [ { "parameter": "BleedDamage", "valueType": "Increase", "value": 0.35 } ]
                        },
                        {
                            "id": "keystone_3",
                            "kind": "Keystone",
                            "x": 2.0,
                            "y": 2.0,
                            "passiveId": "Passive_Skill_Incineration",
                            "contextModifiers": [ { "parameter": "BleedDamage", "valueType": "Increase", "value": 0.35 } ]
                        }
                    ],
                    "edges": []
                }
                """;
            List<string> issues = [];

            PassiveTreeDocument document = PassiveTreeSerializer.Deserialize(json, issues);
            PassiveNode? neighbour = document.Find("keystone_2");

            Assert.IsNull(document.Find("keystone_1"), "a node promising a passive AND lines was kept, half of it unreadable");
            // The pipeline channel is a road to the fighter like the other one, so a passive beside a knob
            // is the same broken promise — the rule asks about lines, not about one list.
            Assert.IsNull(document.Find("keystone_3"), "a passive was allowed to stand beside a context line");
            Assert.IsNotNull(neighbour, "one refused node cost the tree the node beside it");
            Assert.AreEqual(1, neighbour.ContextModifiers.Count, "a keystone speaking in lines alone is legal and must survive untouched");
            Assert.AreEqual(2, issues.Count, string.Join("; ", issues));
            StringAssert.Contains(issues[0], "keystone_1");
            StringAssert.Contains(issues[1], "keystone_3");
        }

        /// <summary>An id that is only whitespace names no passive: the node keeps its lines, is not
        /// refused as speaking both ways, and the blank never goes back into the file as a key.</summary>
        [TestMethod]
        public void ABlankPassiveIdIsNoPassiveAtAll()
        {
            const string json = """
                {
                    "version": 1,
                    "budget": 10,
                    "nodes": [
                        {
                            "id": "small_1",
                            "kind": "Small",
                            "x": 0.0,
                            "y": 0.0,
                            "passiveId": "   ",
                            "modifiers": [ { "parameter": "Armor", "valueType": "Increase", "value": 0.06 } ]
                        }
                    ],
                    "edges": []
                }
                """;
            List<string> issues = [];

            PassiveTreeDocument document = PassiveTreeSerializer.Deserialize(json, issues);
            PassiveNode? node = document.Find("small_1");

            Assert.AreEqual(0, issues.Count, string.Join("; ", issues));
            Assert.IsNotNull(node, "a node naming no passive was refused for speaking both ways");
            Assert.IsNull(node.PassiveId, "a blank id came back as a passive nobody can build");
            Assert.IsFalse(node.IsPassive);
            Assert.AreEqual(1, node.Modifiers.Count, "the node lost the line it actually carries");
            Assert.IsFalse(PassiveTreeSerializer.Serialize(document).Contains("passiveId"),
                "a blank id was written back into the file as a key");
            Assert.IsNull(new PassiveNode { PassiveId = string.Empty }.PassiveId);
        }

        /// <summary>The same rule on the author's screen. The reader throws such a node away WITH the edges
        /// leaning on it, so a tool that saved one would cost the author a corner of his tree at the next
        /// load — the report has to name it before it is written.</summary>
        [TestMethod]
        public void TheAuthoringReportNamesANodeSpeakingBothWays()
        {
            var document = new PassiveTreeDocument();
            var node = new PassiveNode
            {
                Id = "keystone_1", Kind = PassiveNodeKind.Keystone, PassiveId = "Passive_Skill_Porcupine"
            };
            node.ContextModifiers.Add(new ContextModifierLine { Parameter = ContextParameter.BleedDamage, Value = 0.1f });
            document.AddNode(node);

            List<string> issues = document.Validate();

            Assert.IsTrue(issues.Any(issue => issue.Contains("keystone_1") && issue.Contains("one way or the other")),
                "the report accepted a node the reader will refuse whole: " + string.Join("; ", issues));
        }

        /// <summary>Numbers authored for a passive the node never names. The factory that would read them is
        /// never called, so they are balance sitting dead in the file — said out loud, but not at the price
        /// of the node: a typo in an id must not cost the player a point's worth of tree.</summary>
        [TestMethod]
        public void TheAuthoringReportNamesNumbersWithNoPassiveToReadThem_AndKeepsTheNode()
        {
            var document = new PassiveTreeDocument();
            var node = new PassiveNode { Id = "keystone_1", Kind = PassiveNodeKind.Keystone };
            node.Properties["damagePercent"] = 0.25f;
            document.AddNode(node);

            List<string> issues = document.Validate();

            Assert.IsTrue(issues.Any(issue => issue.Contains("keystone_1") && issue.Contains("nobody reads them")),
                "numbers nothing will ever read passed the report in silence: " + string.Join("; ", issues));
            Assert.IsNotNull(document.Find("keystone_1"), "a report cost the author his node");
        }

        /// <summary>A passive IS the node's payload, so the per-class floor on lines has nothing to ask it —
        /// otherwise every converted node would be reported empty for saying what it says another way.</summary>
        [TestMethod]
        public void APassiveNodeIsNotCalledEmptyForCarryingNoLines()
        {
            var document = new PassiveTreeDocument();
            document.AddNode(new PassiveNode
            {
                Id = "small_1", Kind = PassiveNodeKind.Small, Stance = Stance.Strength, PassiveId = "Passive_Skill_Soulless"
            });

            List<string> issues = document.Validate();

            Assert.IsFalse(issues.Any(issue => issue.Contains("small_1") && issue.Contains("at least")),
                "a node granting a passive was reported as having nothing on it: " + string.Join("; ", issues));
        }

        /// <summary>The passive reaches the fighter by its own road, so the parametric channel has nothing
        /// to hand over for it — a taken passive node must not add a parameter to the source, and must not
        /// cost the node beside it its own.</summary>
        [TestMethod]
        public void ATakenPassiveNodeAddsNothingToTheParametricChannel()
        {
            var document = new PassiveTreeDocument();
            var passive = new PassiveNode
            {
                Id = "keystone_1", Kind = PassiveNodeKind.Keystone, PassiveId = "Passive_Skill_Execute"
            };
            passive.Properties["threshold"] = 0.2f;
            var lined = new PassiveNode { Id = "small_1", Kind = PassiveNodeKind.Small };
            lined.Modifiers.Add(new ModifierLine { Parameter = EntityParameter.Armor, ValueType = ModifierValueType.Increase, Value = 0.06f });
            document.AddNode(passive);
            document.AddNode(lined);

            var source = new PassiveTreeParameterSource(new ConditionProvider(ConditionParser.Default()));
            source.Rebuild(document, ["keystone_1", "small_1"]);

            CollectionAssert.AreEquivalent(new[] { EntityParameter.Armor }, source.AffectedParameters.ToArray(),
                "the passive channel leaked into the parametric one");
        }

        /// <summary>A node with no LINES still has something to print — the passive it hands over — and
        /// nothing at all to add to the totals: the passive reaches the fighter as a skill, not as records
        /// the summary could count. Both readers have to keep standing.
        /// <para>The wording of a passive written as a class is its own hand-written rule text, read out
        /// under its <c>_Description</c> key; with no catalog behind the reader the key comes back, the way
        /// every other miss in this class does.</para></summary>
        [TestMethod]
        public void APassiveNodeWithNoLinesPrintsAndSumsWithoutFalling()
        {
            var document = new PassiveTreeDocument();
            var node = new PassiveNode
            {
                Id = "keystone_1",
                Kind = PassiveNodeKind.Keystone,
                Title = "Porcupine",
                Description = "returns what it takes",
                PassiveId = "Passive_Skill_Porcupine"
            };
            node.Properties["damagePercent"] = 0.25f;
            document.AddNode(node);

            TreeSummary summary = PassiveTreeSummary.Build(document, ["keystone_1"], NoBaseline.Instance);

            Assert.AreEqual(0, node.LineCount);

            List<PassiveNodeLine> printed = PassiveNodeLines.Of(node, null, null, null);
            Assert.AreEqual(1, printed.Count, "the passive the node hands over went unsaid");
            Assert.AreEqual("Passive_Skill_Porcupine_Description", printed[0].Text);
            Assert.IsFalse(printed[0].IsConditional, "a passive was marked as gated — gates live on lines");
            Assert.AreEqual("Porcupine", PassiveNodeLines.TitleOf(node, null));
            Assert.AreEqual(0, summary.Parameters.Count);
            Assert.AreEqual(0, summary.Context.Count);
            Assert.AreEqual(1, summary.Keystones.Count, "a keystone granting a passive vanished from the summary");
            Assert.IsFalse(summary.IsEmpty);
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

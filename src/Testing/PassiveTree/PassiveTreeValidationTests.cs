namespace LastBreathTest.PassiveTree
{
    using Core.Data.GameData;
    using Core.Enums;
    using Core.PassiveTree;

    /// <summary>
    /// What the content rules say about the tree the game actually ships. Two halves that need each
    /// other: the authored file must pass without a single claim — a rule describing no real tree is a
    /// rule nobody can act on, and until this ran nothing ever put the two side by side — and the hub
    /// the wheel is built around stays legal for the reasons it was authored, not by the rules going
    /// quiet about start points in general.
    /// </summary>
    [TestClass]
    public class PassiveTreeValidationTests
    {
        private const string HubId = "hub";

        private const string OrphanId = "small_orphan";

        private static PassiveTreeDocument ShippedTree()
        {
            var provider = new PassiveTreeProvider();
            new GameDataService(new FileSystemDataSource(SharedData.Root()), [provider]).LoadAll();
            return provider.Tree;
        }

        /// <summary>A start belonging to no stance — the wheel's centre, as <see cref="NodeKindRules.IsWheelHub"/>
        /// defines it. The tests build one instead of naming the id the shipped file gives it.</summary>
        private static PassiveNode Hub(string id) => new() { Id = id, Kind = PassiveNodeKind.Start };

        /// <summary>The smallest legal wheel: the hub in the middle, one seed per stance hanging off it.
        /// The shape the shipped tree grows out of, small enough to break one way at a time.</summary>
        private static PassiveTreeDocument Wheel()
        {
            var document = new PassiveTreeDocument();
            document.AddNode(Hub(HubId));

            foreach (Stance stance in Enum.GetValues<Stance>())
            {
                var seed = new PassiveNode
                {
                    Id = SeedId(stance),
                    Kind = PassiveNodeKind.Start,
                    Stance = stance,
                    AbilityId = $"Ability_{stance}"
                };

                document.AddNode(seed);
                document.Link(HubId, seed.Id);
            }

            return document;
        }

        private static string SeedId(Stance stance) => $"seed_{stance}";

        [TestMethod]
        public void TheShippedTreePassesValidationWithoutASingleClaim()
        {
            PassiveTreeDocument tree = ShippedTree();
            Assert.IsFalse(tree.IsEmpty, "the shipped catalog produced no tree, so there is nothing to validate");

            List<string> issues = tree.Validate();

            Assert.AreEqual(0, issues.Count, string.Join("; ", issues));
        }

        /// <summary>The hub is recognised by what it is and never by the id it happens to carry, so
        /// renaming it in the file stays content and does not become a broken test.</summary>
        [TestMethod]
        public void TheShippedTreeCarriesOneHubAndOneSeedPerStance()
        {
            PassiveTreeDocument tree = ShippedTree();

            PassiveNode[] hubs = [.. tree.Nodes.Where(NodeKindRules.IsWheelHub)];
            PassiveNode[] seeds = [.. tree.Nodes.Where(NodeKindRules.IsStanceSeed)];

            Assert.AreEqual(1, hubs.Length, "the wheel has one centre");
            Assert.AreEqual(PassiveTreeDocument.StartPointCount, seeds.Length,
                "the hub was counted as a stance start, or a stance lost its seed");
            Assert.AreEqual(string.Empty, hubs[0].AbilityId, "the neutral centre hands out an ability");
        }

        [TestMethod]
        public void AHubWithNeitherStanceNorAbilityIsLegal()
        {
            List<string> issues = Wheel().Validate();

            Assert.AreEqual(0, issues.Count, string.Join("; ", issues));
        }

        /// <summary>The hub is not a fourth seed: it must not fill in for a stance that lost its own, or
        /// the count would go quiet exactly when a stance has no way into the tree.</summary>
        [TestMethod]
        public void TheHubDoesNotStandInForAMissingStanceSeed()
        {
            PassiveTreeDocument document = Wheel();
            document.RemoveNode(SeedId(Stance.Intelligence));

            List<string> issues = document.Validate();

            Assert.IsTrue(issues.Any(issue => issue.Contains("2 stance start point(s)")),
                "the hub was counted as a stance start: " + string.Join("; ", issues));
        }

        [TestMethod]
        public void ASecondHubIsReported()
        {
            PassiveTreeDocument document = Wheel();
            const string second = HubId + "_2";
            document.AddNode(Hub(second));
            document.Link(HubId, second);

            List<string> issues = document.Validate();

            Assert.IsTrue(issues.Any(issue => issue.Contains("2 wheel hub(s)")),
                "a wheel with two centres passed: " + string.Join("; ", issues));
        }

        /// <summary>An ability named on the hub would be handed to every character outside any stance —
        /// a free unlock whose only intended source is a stance seed.</summary>
        [TestMethod]
        public void AHubNamingAnAbilityIsReported()
        {
            PassiveTreeDocument document = Wheel();
            PassiveNode? hub = document.Find(HubId);
            Assert.IsNotNull(hub);
            hub.AbilityId = "Ability_Double_Strike";

            List<string> issues = document.Validate();

            Assert.IsTrue(issues.Any(issue => issue.Contains(HubId) && issue.Contains("stance")),
                "the hub gave an ability away and nothing said so: " + string.Join("; ", issues));
        }

        /// <summary>A stance seed keeps every rule it always had — the exemption is the hub's alone.</summary>
        [TestMethod]
        public void AStanceSeedStillHasToNameAnAbility()
        {
            PassiveTreeDocument document = Wheel();
            PassiveNode? seed = document.Find(SeedId(Stance.Strength));
            Assert.IsNotNull(seed);
            seed.AbilityId = string.Empty;

            List<string> issues = document.Validate();

            Assert.IsTrue(issues.Any(issue => issue.Contains(seed.Id) && issue.Contains("ability")),
                "a stance seed lost its ability and nothing said so: " + string.Join("; ", issues));
        }

        /// <summary>
        /// Numbers left behind by a passive that was named and then cleared. Nothing in the game refuses
        /// such a node — it simply hands the player less than the file looks like it promises — so the
        /// report is the only place the author can ever find out, and the authoring tool shows the same
        /// sentence from the same rule.
        /// </summary>
        [TestMethod]
        public void PropertiesOnANodeThatNamesNoPassiveAreReported()
        {
            PassiveTreeDocument document = Wheel();
            document.AddNode(Orphan());
            document.Link(HubId, OrphanId);

            List<string> issues = document.Validate();

            Assert.IsTrue(issues.Any(issue => issue.Contains(OrphanId) && issue.Contains("names no passive")),
                "stranded numbers passed unmentioned: " + string.Join("; ", issues));
        }

        /// <summary>Stranded numbers stay stranded on a node speaking in lines — the channel it speaks on
        /// says nothing about who reads its properties, and lines are exactly where nobody looks.</summary>
        [TestMethod]
        public void PropertiesAreStillReportedOnANodeCarryingLines()
        {
            PassiveTreeDocument document = Wheel();
            PassiveNode orphan = Orphan();
            orphan.Modifiers.Add(new ModifierLine
            {
                Parameter = EntityParameter.Armor,
                ValueType = ModifierValueType.Increase,
                Value = 0.06f
            });

            document.AddNode(orphan);
            document.Link(HubId, OrphanId);

            List<string> issues = document.Validate();

            Assert.IsTrue(issues.Any(issue => issue.Contains(OrphanId) && issue.Contains("names no passive")),
                "lines hid the stranded numbers: " + string.Join("; ", issues));
        }

        /// <summary>A node carrying tuning numbers and naming no passive to spend them on.</summary>
        private static PassiveNode Orphan()
        {
            var node = new PassiveNode { Id = OrphanId, Kind = PassiveNodeKind.Small };
            node.Properties["percentFromDamage"] = 0.75f;

            return node;
        }

        /// <summary>A line on the hub is still a line the class cannot carry: the centre is exempt from
        /// the stance and ability rules, not from the content limits.</summary>
        [TestMethod]
        public void AHubCarryingAModifierLineIsStillReported()
        {
            PassiveTreeDocument document = Wheel();
            document.Find(HubId)?.Modifiers.Add(new ModifierLine
            {
                Parameter = EntityParameter.Armor,
                ValueType = ModifierValueType.Increase,
                Value = 0.06f
            });

            List<string> issues = document.Validate();

            Assert.IsTrue(issues.Any(issue => issue.Contains(HubId) && issue.Contains("at most 0")),
                "the hub carried content nobody budgeted: " + string.Join("; ", issues));
        }
    }
}

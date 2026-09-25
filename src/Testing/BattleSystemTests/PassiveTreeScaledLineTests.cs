namespace LastBreathTest.BattleSystemTests
{
    using Core.Data.GameData;
    using Core.Enums;
    using Core.Localization;
    using Core.Modifiers;
    using Core.Modifiers.Conditions;
    using Core.PassiveTree;
    using Core.PassiveTree.Allocation;
    using Core.PassiveTree.Summary;
    using Core.PassiveTree.View;

    /// <summary>
    /// A node line worth its value FOR EVERY UNIT of a carrier parameter ("+1% physical damage per point
    /// of Strength"). Three things have to hold at once: the file keeps the carrier through a round trip
    /// and refuses a nonsense one, the contribution is re-measured whenever the carrier's parameter moves
    /// and leaves nothing behind when the node goes, and the sentence says "per Strength" out loud —
    /// a scale printed as a flat bonus is a lie the player pays for.
    /// </summary>
    [TestClass]
    public class PassiveTreeScaledLineTests
    {
        private const string Seed = "start";
        private const string Node = "small_1";
        private const string Keystone = "keystone_6";
        private const float BaseDamage = 100f;
        private const float PerUnit = 0.01f;
        private const float Strength = 10f;
        private const float GearStrength = 2f;
        private const float MaxHealth = 100f;
        private const float Tolerance = 0.001f;

        [TestMethod]
        public void TheLineIsWorthTheCarriersScaleAndFollowsIt()
        {
            ConditionOwner fighter = Carrier(out _, Scaled(condition: string.Empty));

            Assert.AreEqual(BaseDamage * (1f + PerUnit * Strength), Damage(fighter), Tolerance,
                "the line was not measured against the carrier it names");

            // The way strength actually moves in play: a piece of gear, not an authored base value.
            fighter.ParameterModifiers.AddModifier(new SimpleModifier(EntityParameter.Strength, ModifierValueType.Flat, GearStrength, "gear"));

            Assert.AreEqual(BaseDamage * (1f + PerUnit * (Strength + GearStrength)), Damage(fighter), Tolerance,
                "the carrier grew stronger and the line kept the measure it was bound with");
        }

        [TestMethod]
        public void WithoutACarrier_TheLineIsWorthNothing()
        {
            IPassiveTreeService service = Service(Scaled(condition: string.Empty), ConditionCatalogs.Empty());
            var fighter = new ConditionOwner();
            Seeded(fighter);
            fighter.ParameterModifiers.RegisterSource(service.ParameterSource);

            Assert.AreEqual(BaseDamage, Damage(fighter), Tolerance,
                "a line with nobody to measure handed out its per-unit figure as an outright bonus");
        }

        [TestMethod]
        public void RefundingTheNode_TakesTheContributionAndTheWatchWithIt()
        {
            var fighter = new ConditionOwner();
            Seeded(fighter);
            int alone = fighter.ListenerCount;

            IPassiveTreeService service = Service(Scaled(condition: string.Empty), ConditionCatalogs.Empty());
            fighter.ParameterModifiers.RegisterSource(service.ParameterSource);
            service.ContextSource.Attach(fighter);

            Assert.AreEqual(BaseDamage * (1f + PerUnit * Strength), Damage(fighter), Tolerance, "the fixture never got the bonus on");
            Assert.IsTrue(fighter.ListenerCount > alone, "the line never subscribed to the parameter it measures");

            service.Respec();

            Assert.AreEqual(BaseDamage, Damage(fighter), Tolerance, "the refunded line kept scaling off the carrier");
            Assert.AreEqual(alone, fighter.ListenerCount, "the refunded line is still watching the carrier's parameter");
        }

        [TestMethod]
        public void MovingToASecondFighter_TakesTheScaleAndTheWatchWithIt()
        {
            // The scene change as the game makes it: world and arena build separate player objects out of
            // one tree service, and the departing one lets the pulled source go on its way out.
            IPassiveTreeService service = Service(Scaled(condition: string.Empty), ConditionCatalogs.Empty());
            ConditionOwner first = Carrier(service);
            ConditionOwner second = Carrier(service);
            int alone = first.ListenerCount;

            service.ContextSource.Attach(first);
            Assert.AreEqual(BaseDamage * (1f + PerUnit * Strength), Damage(first), Tolerance, "the fixture never got the bonus on");

            first.ParameterModifiers.UnregisterSource(service.ParameterSource);
            service.ContextSource.Attach(second);

            Assert.AreEqual(BaseDamage, Damage(first), Tolerance, "the fighter the tree left kept its scaled line");
            Assert.AreEqual(alone, first.ListenerCount, "the tree is still measuring a fighter it let go");
            Assert.AreEqual(BaseDamage * (1f + PerUnit * Strength), Damage(second), Tolerance);
        }

        [TestMethod]
        public void AScaleHeldUpByAGate_CountsOnlyWhileTheGateDoes()
        {
            ConditionOwner fighter = Carrier(out _, Scaled(ConditionCatalogs.WhileWounded), ConditionCatalogs.Wounded());

            Assert.AreEqual(BaseDamage, Damage(fighter), Tolerance, "the line counted before its gate did");

            Wound(fighter);
            Assert.AreEqual(BaseDamage * (1f + PerUnit * Strength), Damage(fighter), Tolerance,
                "the gate came true and the scale was never measured");

            Heal(fighter);
            Assert.AreEqual(BaseDamage, Damage(fighter), Tolerance, "the gate let go and the scale stayed on the fighter");
        }

        [TestMethod]
        public void TheFileKeepsTheCarrier_AndItsAbsence()
        {
            var document = new PassiveTreeDocument();
            var node = new PassiveNode { Id = Node, Kind = PassiveNodeKind.Small, Stance = Stance.Strength };
            node.Modifiers.Add(Scaled(condition: string.Empty));
            node.Modifiers.Add(new ModifierLine { Parameter = EntityParameter.Armor, ValueType = ModifierValueType.Flat, Value = 5f });
            document.AddNode(node);

            string json = PassiveTreeSerializer.Serialize(document);
            List<string> issues = [];
            PassiveTreeDocument read = PassiveTreeSerializer.Deserialize(json, issues);

            Assert.AreEqual(0, issues.Count, string.Join("; ", issues));
            StringAssert.Contains(json, "\"perParameter\": \"Strength\"", "the carrier was not written to the file");
            Assert.AreEqual(EntityParameter.Strength, read.Find(Node)!.Modifiers[0].PerParameter);
            Assert.IsNull(read.Find(Node)!.Modifiers[1].PerParameter, "an ordinary line came back carrying a scale nobody authored");
        }

        [TestMethod]
        public void ATypoInTheCarrier_CostsTheLineInsteadOfBecomingTheFirstMember()
        {
            List<string> issues = [];
            PassiveTreeDocument document = PassiveTreeSerializer.Deserialize(Written("PhysicalDamage", "Strenght"), issues);

            Assert.AreEqual(1, issues.Count, "a misspelled carrier was accepted");
            Assert.AreEqual(0, document.Find(Node)!.Modifiers.Count, "the refused line was kept anyway");
        }

        [TestMethod]
        public void ALineScaledPerUnitOfItself_IsRefused()
        {
            List<string> issues = [];
            PassiveTreeDocument document = PassiveTreeSerializer.Deserialize(Written("PhysicalDamage", "PhysicalDamage"), issues);

            Assert.AreEqual(1, issues.Count, "a line feeding the very parameter it measures was accepted");
            Assert.AreEqual(0, document.Find(Node)!.Modifiers.Count);
        }

        [TestMethod]
        public void AnAggregateFamilyOnBothEnds_IsRefused()
        {
            // The loop the self-check alone does not see: a change on an aggregate is fanned out to every
            // member, so an aggregate line measured per unit of one of its members resolves itself.
            List<string> issues = [];
            PassiveTreeDocument family = PassiveTreeSerializer.Deserialize(Written("AllAttribute", "Strength"), issues);

            Assert.AreEqual(1, issues.Count, "an aggregate scaled per unit of its own member was accepted");
            Assert.AreEqual(0, family.Find(Node)!.Modifiers.Count);

            List<string> reversed = [];
            PassiveTreeDocument member = PassiveTreeSerializer.Deserialize(Written("Strength", "AllAttribute"), reversed);

            Assert.AreEqual(1, reversed.Count, "a member scaled per unit of the aggregate that fans onto it was accepted");
            Assert.AreEqual(0, member.Find(Node)!.Modifiers.Count);
        }

        [TestMethod]
        public void AnAggregateAsTheCarrier_IsRefused()
        {
            // Nobody ever holds a value for an aggregate, so the line would measure zero forever and say
            // nothing about why. Refused where it is written instead.
            List<string> issues = [];
            PassiveTreeDocument document = PassiveTreeSerializer.Deserialize(Written("PhysicalDamage", "AllResistance"), issues);

            Assert.AreEqual(1, issues.Count, "an aggregate was accepted as something to measure against");
            Assert.AreEqual(0, document.Find(Node)!.Modifiers.Count);
        }

        [TestMethod]
        public void TwoLinesMeasuringEachOther_SettleInsteadOfRecursing()
        {
            // Each resolve announces the parameter the other one measures. Without the latch on the
            // announcement this walk takes the process down with it — the values are only worth reading
            // because the test returned at all.
            var document = new PassiveTreeDocument();
            document.AddNode(new PassiveNode { Id = Seed, Kind = PassiveNodeKind.Start, Stance = Stance.Strength });

            var node = new PassiveNode { Id = Node, Kind = PassiveNodeKind.Small, Stance = Stance.Strength };
            node.Modifiers.Add(Mutual(EntityParameter.PhysicalDamage, EntityParameter.Strength));
            node.Modifiers.Add(Mutual(EntityParameter.Strength, EntityParameter.PhysicalDamage));
            document.AddNode(node);
            document.Link(Seed, Node);

            var service = new PassiveTreeService(new ScaledTreeProvider(document), ConditionCatalogs.Empty());
            service.SetTotalPoints(1);
            Assert.AreEqual(AllocationResult.Success, service.Take(Node));

            ConditionOwner fighter = Carrier(service);
            service.ContextSource.Attach(fighter);
            fighter.ParameterModifiers.AddModifier(new SimpleModifier(EntityParameter.Strength, ModifierValueType.Flat, GearStrength, "gear"));

            Assert.IsTrue(float.IsFinite(Damage(fighter)) && Damage(fighter) >= BaseDamage, $"physical damage ran away: {Damage(fighter)}");
            Assert.IsTrue(float.IsFinite(Scale(fighter)) && Scale(fighter) >= Strength, $"strength ran away: {Scale(fighter)}");
        }

        [TestMethod]
        public void CopyingALine_CarriesTheScale()
        {
            // What the authoring tool's snapshots do field for field: a copy that dropped the carrier
            // would strip a hand-written line the first time its node was edited, and say nothing.
            ModifierLine copy = Scaled(ConditionCatalogs.WhileWounded).Copy();

            Assert.AreEqual(EntityParameter.Strength, copy.PerParameter);
            Assert.AreEqual(ConditionCatalogs.WhileWounded, copy.Condition);
        }

        [TestMethod]
        public void TheSummaryLeavesTheScaledLineOutOfTheColumnsAndCountsIt()
        {
            var document = new PassiveTreeDocument();
            var node = new PassiveNode { Id = Node, Kind = PassiveNodeKind.Small, Stance = Stance.Strength };
            node.Modifiers.Add(Scaled(condition: string.Empty));
            node.Modifiers.Add(new ModifierLine
            {
                Parameter = EntityParameter.PhysicalDamage, ValueType = ModifierValueType.Increase, Value = 0.2f
            });
            document.AddNode(node);

            TreeSummary summary = PassiveTreeSummary.Build(document, [Node], NoBaseline.Instance);
            ParameterTotal damage = summary.Parameters.Single(total => total.Parameter == EntityParameter.PhysicalDamage);

            Assert.AreEqual(1, summary.ScaledLines, "the reading does not say how much of the allocation it left out");
            Assert.AreEqual(0.2f, damage.Increase, Tolerance, "a scale with no carrier behind it was folded into the total anyway");
            Assert.AreEqual(1, damage.Lines);
        }

        [TestMethod]
        public void TheShippedKeystone_SaysItScalesWithStrength()
        {
            var localization = new FakeLocalizationProvider();
            foreach ((string key, string text) in ShippedWording()) localization.Strings[key] = text;

            List<string> issues = [];
            PassiveTreeDocument tree = PassiveTreeSerializer.Load(
                Path.Combine(SharedData.Catalog(DataCatalog.PassiveTree), PassiveTreeFormat.DefaultFileName), issues);
            Assert.AreEqual(0, issues.Count, string.Join("; ", issues));

            List<PassiveNodeLine> lines = PassiveNodeLines.Of(
                tree.Find(Keystone)!, new ModifierFormatter(localization, new ParameterFormatProvider()), knobs: null, localization);

            Assert.AreEqual("+1% increased Physical Damage per Strength", lines[0].Text);
        }

        private static float Damage(ConditionOwner fighter) => fighter.Parameters.GetValueForParameter(EntityParameter.PhysicalDamage);

        private static float Scale(ConditionOwner fighter) => fighter.Parameters.GetValueForParameter(EntityParameter.Strength);

        /// <summary>Half of a pair that measures itself against the other one — the shape that recurses.</summary>
        private static ModifierLine Mutual(EntityParameter parameter, EntityParameter per) => new()
        {
            Parameter = parameter,
            ValueType = ModifierValueType.Increase,
            Value = 0.1f,
            PerParameter = per
        };

        private static void Wound(ConditionOwner fighter) => fighter.CurrentHealth = MaxHealth * (ConditionCatalogs.WoundedShare - 0.1f);

        private static void Heal(ConditionOwner fighter) => fighter.CurrentHealth = MaxHealth;

        /// <summary>The line the whole file is about: one hundredth of physical damage per point of Strength.</summary>
        private static ModifierLine Scaled(string condition) => new()
        {
            Parameter = EntityParameter.PhysicalDamage,
            ValueType = ModifierValueType.Increase,
            Value = PerUnit,
            PerParameter = EntityParameter.Strength,
            Condition = condition
        };

        /// <summary>One node's file text with the two parameters spelled out, for the refusals.</summary>
        private static string Written(string parameter, string perParameter) => $$"""
            {
                "version": 1,
                "nodes": [
                    {
                        "id": "{{Node}}",
                        "kind": "Small",
                        "x": 0,
                        "y": 0,
                        "modifiers": [
                            { "parameter": "{{parameter}}", "valueType": "Increase", "value": 0.01, "perParameter": "{{perParameter}}" }
                        ]
                    }
                ],
                "edges": []
            }
            """;

        /// <summary>A carrier already holding the tree's contribution, with a base to scale against.</summary>
        private static ConditionOwner Carrier(out IPassiveTreeService service, ModifierLine line, ConditionProvider? catalog = null)
        {
            service = Service(line, catalog ?? ConditionCatalogs.Empty());
            ConditionOwner fighter = Carrier(service);
            service.ContextSource.Attach(fighter);

            return fighter;
        }

        private static ConditionOwner Carrier(IPassiveTreeService service)
        {
            var fighter = new ConditionOwner();
            Seeded(fighter);
            fighter.ParameterModifiers.RegisterSource(service.ParameterSource);

            return fighter;
        }

        /// <summary>A fighter with something to measure and something to scale: full health, ten Strength
        /// and a hundred physical damage under the tree.</summary>
        private static void Seeded(ConditionOwner fighter)
        {
            fighter.SetMaximum(EntityParameter.Health, MaxHealth);
            fighter.CurrentHealth = MaxHealth;
            fighter.Parameters.SetBaseValueForParameter(EntityParameter.Strength, Strength);
            fighter.Parameters.SetBaseValueForParameter(EntityParameter.PhysicalDamage, BaseDamage);
        }

        private static IPassiveTreeService Service(ModifierLine line, ConditionProvider catalog)
        {
            var document = new PassiveTreeDocument();
            document.AddNode(new PassiveNode { Id = Seed, Kind = PassiveNodeKind.Start, Stance = Stance.Strength });

            var node = new PassiveNode { Id = Node, Kind = PassiveNodeKind.Small, Stance = Stance.Strength };
            node.Modifiers.Add(line);
            document.AddNode(node);
            document.Link(Seed, Node);

            var service = new PassiveTreeService(new ScaledTreeProvider(document), catalog);
            service.SetTotalPoints(1);
            Assert.AreEqual(AllocationResult.Success, service.Take(Node), "the fixture never bought its node");

            return service;
        }

        /// <summary>en.po as key → wording, so the sentence under test is the one the game ships.</summary>
        private static Dictionary<string, string> ShippedWording()
        {
            string[] lines = File.ReadAllLines(Path.Combine(SharedData.Root(), "Localization", "en.po"));
            Dictionary<string, string> entries = new(StringComparer.Ordinal);

            for (int line = 0; line + 1 < lines.Length; line++)
            {
                if (!Quoted(lines[line], "msgid ", out string id) || id.Length == 0) continue;
                if (Quoted(lines[line + 1], "msgstr ", out string text)) entries[id] = text;
            }

            Assert.IsTrue(entries.Count > 0, "en.po was not read at all, so this walk proves nothing");
            return entries;
        }

        private static bool Quoted(string line, string prefix, out string value)
        {
            value = string.Empty;
            if (!line.StartsWith(prefix + '"', StringComparison.Ordinal) || !line.EndsWith('"')) return false;

            value = line[(prefix.Length + 1)..^1];
            return true;
        }

        private sealed class ScaledTreeProvider(PassiveTreeDocument tree) : IPassiveTreeProvider
        {
            public PassiveTreeDocument Tree { get; } = tree;

            public IReadOnlyList<string> Issues => [];
        }
    }
}

namespace LastBreathTest.BattleSystemTests
{
    using Core.Enums;
    using Core.Modifiers.Context;
    using Core.PassiveTree;
    using Core.PassiveTree.Summary;

    /// <summary>
    /// What a set of taken nodes adds up to. One summator for the authoring tool and the game, and the
    /// one thing they must NOT agree on is what to do with a gated line: the tool has no fighter to test
    /// a condition against and counts it, a screen showing the player his own numbers cannot count a
    /// bonus that is off while he reads it.
    /// </summary>
    [TestClass]
    public class PassiveTreeSummaryTests
    {
        private const string Plain = "plain";
        private const string Gated = "gated";

        private const float Always = 5f;
        private const float OnlyWounded = 7f;
        private const float BaseStrength = 10f;
        private const float Tolerance = 0.0001f;

        [TestMethod]
        public void TheCharacterReading_LeavesGatedLinesOutAndSaysHowMany()
        {
            PassiveTreeDocument document = Tree();

            TreeSummary held = PassiveTreeSummary.BuildUnconditional(document, [Plain, Gated], NoBaseline.Instance);
            ParameterTotal strength = held.Parameters.Single(total => total.Parameter == EntityParameter.Strength);

            Assert.AreEqual(Always, strength.Flat, Tolerance, "a line that only counts sometimes entered a total presented as the truth");
            Assert.AreEqual(Always, strength.Total, Tolerance);
            Assert.AreEqual(1, strength.Lines);

            // Both channels feed the count: a gated line is content whichever road it takes to the
            // fighter, and a tree of context-only conditionals must not lose the warning.
            Assert.AreEqual(3, held.ConditionalLines, "the panel cannot say how much of the allocation it is not showing");
        }

        [TestMethod]
        public void TheAuthoringReading_CountsGatedLinesAndFlagsThem()
        {
            PassiveTreeDocument document = Tree();

            TreeSummary carried = PassiveTreeSummary.Build(document, [Plain, Gated], NoBaseline.Instance);
            ParameterTotal strength = carried.Parameters.Single(total => total.Parameter == EntityParameter.Strength);

            Assert.AreEqual(Always + OnlyWounded, strength.Flat, Tolerance, "the tool stopped seeing what the allocation carries");
            Assert.AreEqual(2, strength.Lines);
            Assert.AreEqual(1, strength.ConditionalLines);
            Assert.AreEqual(3, carried.ConditionalLines, "both channels feed the count");
        }

        /// <summary>A knob is one number to the pipeline, so the gated lines feeding it come off that one
        /// number — and a knob left with nothing but gated lines disappears from the table rather than
        /// showing a zero the player would read as a knob he owns.</summary>
        [TestMethod]
        public void AKnobFedOnlyByGatedLines_IsNotShownToTheCharacterAtAll()
        {
            PassiveTreeDocument document = Tree();

            TreeSummary carried = PassiveTreeSummary.Build(document, [Plain, Gated], NoBaseline.Instance);
            TreeSummary held = PassiveTreeSummary.BuildUnconditional(document, [Plain, Gated], NoBaseline.Instance);

            Assert.AreEqual(2, carried.Context.Single(knob => knob.Parameter == ContextParameter.BleedDamage).Lines);
            Assert.IsFalse(held.Context.Any(knob => knob.Parameter == ContextParameter.HealingEfficiency),
                "a knob nothing unconditional feeds was shown as one the character holds");

            ContextTotal bleed = held.Context.Single(knob => knob.Parameter == ContextParameter.BleedDamage);
            Assert.AreEqual(1, bleed.Lines);
            Assert.AreEqual(0.1f, bleed.Value, Tolerance, "a gated line was added into a knob the character reads as his own");
        }

        /// <summary>The total is resolved through the game's own formula over a baseline, so a panel and
        /// a battle cannot disagree about what a percentage does to a number.</summary>
        [TestMethod]
        public void TheTotalIsResolvedAgainstTheBaselineItIsGiven()
        {
            PassiveTreeDocument document = Tree();

            TreeSummary held = PassiveTreeSummary.BuildUnconditional(document, [Plain], new FixedBaseline(BaseStrength));
            ParameterTotal strength = held.Parameters.Single(total => total.Parameter == EntityParameter.Strength);

            Assert.AreEqual(BaseStrength, strength.BaseValue, Tolerance);
            Assert.AreEqual(BaseStrength + Always, strength.Total, Tolerance);
        }

        /// <summary>The summator has nothing behind it to fold a family back together, unlike the live
        /// contribution, so it has to expand an aggregate itself — once per member and no more.</summary>
        [TestMethod]
        public void AnAggregateLine_LandsOnEveryFamilyMemberExactlyOnce()
        {
            const float grant = 3f;
            var document = new PassiveTreeDocument();
            var node = new PassiveNode { Id = Plain, Kind = PassiveNodeKind.Small };
            node.Modifiers.Add(new ModifierLine
            {
                Parameter = EntityParameter.AllAttribute,
                ValueType = ModifierValueType.Flat,
                Value = grant
            });
            document.AddNode(node);

            TreeSummary summary = PassiveTreeSummary.BuildUnconditional(document, [Plain], NoBaseline.Instance);

            foreach (EntityParameter attribute in (EntityParameter[])[EntityParameter.Strength, EntityParameter.Dexterity, EntityParameter.Intelligence])
            {
                ParameterTotal total = summary.Parameters.Single(entry => entry.Parameter == attribute);
                Assert.AreEqual(grant, total.Flat, Tolerance, $"{attribute} missed the aggregate line or took it twice");
                Assert.AreEqual(1, total.Lines, $"{attribute} folded the aggregate line more than once");
            }

            Assert.IsFalse(summary.Parameters.Any(total => total.Parameter == EntityParameter.AllAttribute),
                "the aggregate stayed in the table beside the members it expanded into");
        }

        /// <summary>Two nodes: one carrying lines that always count, one carrying the same lines gated on
        /// a condition. Both channels, because both reach a fighter and both have to be answered.</summary>
        private static PassiveTreeDocument Tree()
        {
            var document = new PassiveTreeDocument();

            var plain = new PassiveNode { Id = Plain, Kind = PassiveNodeKind.Small };
            plain.Modifiers.Add(new ModifierLine
            {
                Parameter = EntityParameter.Strength, ValueType = ModifierValueType.Flat, Value = Always
            });
            plain.ContextModifiers.Add(new ContextModifierLine
            {
                Parameter = ContextParameter.BleedDamage, ValueType = ModifierValueType.Increase, Value = 0.1f
            });
            document.AddNode(plain);

            var gated = new PassiveNode { Id = Gated, Kind = PassiveNodeKind.Small };
            gated.Modifiers.Add(new ModifierLine
            {
                Parameter = EntityParameter.Strength,
                ValueType = ModifierValueType.Flat,
                Value = OnlyWounded,
                Condition = ConditionCatalogs.WhileWounded
            });
            gated.ContextModifiers.Add(new ContextModifierLine
            {
                Parameter = ContextParameter.BleedDamage,
                ValueType = ModifierValueType.Increase,
                Value = 0.2f,
                Condition = ConditionCatalogs.WhileWounded
            });
            gated.ContextModifiers.Add(new ContextModifierLine
            {
                Parameter = ContextParameter.HealingEfficiency,
                ValueType = ModifierValueType.Increase,
                Value = 0.3f,
                Condition = ConditionCatalogs.WhileWounded
            });
            document.AddNode(gated);

            return document;
        }

        private sealed class FixedBaseline(float value) : IParameterBaseline
        {
            public float Of(EntityParameter parameter) => value;
        }
    }
}

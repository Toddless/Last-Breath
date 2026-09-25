namespace LastBreathTest.BattleSystemTests
{
    using Battle.Source.UIElements.PassiveWheel;
    using Core.Enums;
    using Core.Localization;
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

        /// <summary>The warning counts what the player reads, and a composite is ONE gated line however
        /// many records spell it — across both channels — while its parts still land on their own
        /// parameters.</summary>
        [TestMethod]
        public void AGatedComposite_IsOneLineOfTheWarningAndSeveralLinesOfTheTotals()
        {
            const string composite = "composite";
            const string stamp = "mana";
            var document = new PassiveTreeDocument();
            var node = new PassiveNode { Id = composite, Kind = PassiveNodeKind.Notable };
            node.Modifiers.Add(new ModifierLine
            {
                Parameter = EntityParameter.Strength,
                ValueType = ModifierValueType.Flat,
                Value = OnlyWounded,
                Condition = ConditionCatalogs.WhileWounded,
                GroupId = stamp
            });
            node.Modifiers.Add(new ModifierLine
            {
                Parameter = EntityParameter.Dexterity,
                ValueType = ModifierValueType.Flat,
                Value = OnlyWounded,
                Condition = ConditionCatalogs.WhileWounded,
                GroupId = stamp
            });
            node.ContextModifiers.Add(new ContextModifierLine
            {
                Parameter = ContextParameter.BleedDamage,
                ValueType = ModifierValueType.Increase,
                Value = 0.2f,
                Condition = ConditionCatalogs.WhileWounded,
                GroupId = stamp
            });
            document.AddNode(node);

            TreeSummary carried = PassiveTreeSummary.Build(document, [composite], NoBaseline.Instance);

            Assert.AreEqual(1, carried.ConditionalLines, "one sentence was counted once per record it is spelled by");
            Assert.AreEqual(OnlyWounded, carried.Parameters.Single(total => total.Parameter == EntityParameter.Strength).Flat, Tolerance);
            Assert.AreEqual(OnlyWounded, carried.Parameters.Single(total => total.Parameter == EntityParameter.Dexterity).Flat, Tolerance,
                "a part of the composite stopped reaching its own parameter");
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

    /// <summary>
    /// What the totals panel prints in a row's cells. The panel has no base value and no gear behind it,
    /// so a line lives in the bucket it was written in and nowhere else — a percent quietly re-read as a
    /// flat number is how a node worth 40% came to read as nothing at all.
    /// </summary>
    [TestClass]
    public class PassiveSummaryCellsTests
    {
        private readonly ModifierFormatter _formatter =
            new(new FakeLocalizationProvider(), new ParameterFormatProvider());

        [TestMethod]
        public void APercentLine_FillsItsOwnBucketAndLeavesTheOthersEmpty()
        {
            (string flat, string increase, string more) = PassiveSummaryCells.Buckets(_formatter, Total(increase: 0.4f));

            Assert.AreEqual(string.Empty, flat, "a percent line was quoted as a flat amount");
            Assert.AreEqual("40%", increase);
            Assert.AreEqual(string.Empty, more, "an increase landed under the multiplier heading");
        }

        [TestMethod]
        public void APlanOfPercentNodes_StillPreviewsWhatItWouldAdd() =>
            Assert.AreEqual("+40%", PassiveSummaryCells.PlanDelta(_formatter, Total(), Total(increase: 0.4f)),
                "the preview said nothing about a plan the player is about to pay for");

        /// <summary>Three buckets cannot be added together without a fighter to fold them against, so the
        /// preview quotes each in its own units and marks the multiplier one.</summary>
        [TestMethod]
        public void APlanTouchingSeveralBuckets_QuotesEachOfThem() =>
            Assert.AreEqual("+20, +10%, ×5%",
                PassiveSummaryCells.PlanDelta(_formatter, Total(), Total(20f, 0.1f, 0.05f)));

        [TestMethod]
        public void APlanThatChangesNothing_PreviewsNothing() =>
            Assert.AreEqual(string.Empty, PassiveSummaryCells.PlanDelta(_formatter, Total(increase: 0.4f), Total(increase: 0.4f)));

        private static ParameterTotal Total(float flat = 0f, float increase = 0f, float more = 0f) =>
            new(EntityParameter.Evade, flat, increase, more, 0f, flat, 1, 0);
    }
}

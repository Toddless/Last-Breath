namespace LastBreathTest.BattleSystemTests
{
    using Core.Enums;
    using Core.Items;
    using Core.Localization;
    using Core.Modifiers;

    /// <summary>
    /// The UI line contract: parts of one composite roll (shared GroupId) present as a SINGLE row whose
    /// id is the first part (the group-reroll target), the Alt reveal appends each part's roll spread, and
    /// a row a condition holds up says which condition — a bonus that only counts sometimes must not read
    /// like one the player always has.
    /// </summary>
    [TestClass]
    public class EquipItemLinesTests
    {
        [TestInitialize]
        public void Setup()
        {
            var provider = new FakeLocalizationProvider();
            provider.Strings["Modifier_Flat"] = "{value} {parameter}";
            provider.Strings["Modifier_Increase"] = "{value} increased {parameter}";
            provider.Strings["Modifier_Flat_Range"] = "{min}–{max} {parameter}";
            provider.Strings["Modifier_Increase_Range"] = "{min}–{max} increased {parameter}";
            provider.Strings["Context_Modifier_HealingEfficiency"] = "Increases healing efficiency by {value}";
            provider.Strings["Modifier_Conditional"] = "{line} {condition}";
            provider.Strings[ConditionalLineText.ClauseKey(TestConditions.Wounded)] = "while wounded";
            provider.Strings["Strength"] = "Strength";
            provider.Strings["Intelligence"] = "Intelligence";
            provider.Strings["Evade"] = "Evade";

            var modifierFormatter = new ModifierFormatter(provider, new ParameterFormatProvider());
            var contextFormatter = new ContextModifierFormatter(provider);
            Localization.Override(new LocalizationService(provider, modifierFormatter, contextFormatter,
            [
                new ModifierTextFormatter(modifierFormatter),
                new ContextModifierTextFormatter(contextFormatter),
                new ModifierDescriptorTextFormatter(modifierFormatter, contextFormatter),
            ]));
        }

        [TestMethod]
        public void GroupedPartsJoinIntoOneRowTargetingTheFirstPart()
        {
            var item = new EquipItem(EquipmentPiece.Ring, "Band", []);
            var partA = new SimpleModifier(EntityParameter.Strength, ModifierValueType.Flat, 5f, "test") { GroupId = "g" };
            var partB = new SimpleModifier(EntityParameter.Intelligence, ModifierValueType.Flat, 3f, "test") { GroupId = "g" };
            var loner = new SimpleModifier(EntityParameter.Evade, ModifierValueType.Flat, 2f, "test");
            item.AddAdditionalModifier(partA);
            item.AddAdditionalModifier(partB);
            item.AddAdditionalModifier(loner);

            var rows = EquipItemLines.ComposeRolled(item);

            Assert.AreEqual(2, rows.Count);
            Assert.AreEqual("+5 Strength, +3 Intelligence", rows[0].Text);
            Assert.AreEqual(partA.InstanceId, rows[0].InstanceId, "The row must target the FIRST part of the group.");
            Assert.AreEqual("+2 Evade", rows[1].Text);
        }

        [TestMethod]
        public void GroupSpanningBothChannelsStillRendersAsOneRow()
        {
            var item = new EquipItem(EquipmentPiece.Ring, "Band", []);
            var entityPart = new SimpleModifier(EntityParameter.Strength, ModifierValueType.Flat, 5f, "test") { GroupId = "g" };
            var contextPart = new ContextModifierEntry(ContextParameter.HealingEfficiency, ModifierValueType.Increase, 0.2f) { GroupId = "g" };
            item.AddAdditionalModifier(entityPart);
            item.AddAdditionalContextModifier(contextPart);

            var rows = EquipItemLines.ComposeRolled(item);

            Assert.AreEqual(1, rows.Count);
            Assert.AreEqual("+5 Strength, Increases healing efficiency by 20%", rows[0].Text);
            Assert.AreEqual(entityPart.InstanceId, rows[0].InstanceId);
        }

        [TestMethod]
        public void RowsComeGroupedByAffixFamily_GiftLast()
        {
            var item = new EquipItem(EquipmentPiece.Ring, "Band", []);
            item.AddAdditionalModifier(Line(EntityParameter.Evade, AffixKind.Mythic));
            item.AddAdditionalModifier(Line(EntityParameter.Intelligence, AffixKind.Suffix));
            item.AddAdditionalModifier(Line(EntityParameter.Strength, AffixKind.None)); // legacy save, no family
            item.AddAdditionalModifier(Line(EntityParameter.Evade, AffixKind.Prefix));

            var rows = EquipItemLines.ComposeRolled(item);

            CollectionAssert.AreEqual(
                new[] { AffixKind.Prefix, AffixKind.Suffix, AffixKind.None, AffixKind.Mythic },
                rows.Select(row => row.Affix).ToArray(),
                "Blocks must read prefixes, suffixes, family-less leftovers, then the ascension gift.");
        }

        [TestMethod]
        public void RowsKeepTheItemOrderInsideABlock()
        {
            // The sort is stable: a rerolled line put back into its slot must display in that slot,
            // not at the bottom of its family.
            var item = new EquipItem(EquipmentPiece.Ring, "Band", []);
            item.AddAdditionalModifier(Line(EntityParameter.Strength, AffixKind.Prefix));
            item.AddAdditionalModifier(Line(EntityParameter.Intelligence, AffixKind.Prefix));
            item.AddAdditionalModifier(Line(EntityParameter.Evade, AffixKind.Prefix));

            var rows = EquipItemLines.ComposeRolled(item);

            CollectionAssert.AreEqual(
                new[] { "+5 Strength", "+5 Intelligence", "+5 Evade" },
                rows.Select(row => row.Text).ToArray());
        }

        [TestMethod]
        public void GroupedRowCarriesTheFamilyOfItsParts()
        {
            var item = new EquipItem(EquipmentPiece.Ring, "Band", []);
            var partA = new SimpleModifier(EntityParameter.Strength, ModifierValueType.Flat, 5f, "test") { GroupId = "g", Affix = AffixKind.Suffix };
            var partB = new SimpleModifier(EntityParameter.Intelligence, ModifierValueType.Flat, 3f, "test") { GroupId = "g", Affix = AffixKind.Suffix };
            item.AddAdditionalModifier(partA);
            item.AddAdditionalModifier(partB);

            var rows = EquipItemLines.ComposeRolled(item);

            Assert.AreEqual(1, rows.Count);
            Assert.AreEqual(AffixKind.Suffix, rows[0].Affix, "A composite row belongs to the family stamped on its parts.");
        }

        [TestMethod]
        public void RevealedTextAppendsTheSpreadOnlyToRolledParts()
        {
            var item = new EquipItem(EquipmentPiece.Ring, "Band", []);
            var rolled = new SimpleModifier(EntityParameter.Strength, ModifierValueType.Flat, 47f, "test") { RolledRange = new ValueRange(40f, 60f) };
            item.AddAdditionalModifier(rolled);

            var rows = EquipItemLines.ComposeRolled(item);

            Assert.AreEqual("+47 Strength", rows[0].Text);
            Assert.AreEqual("+47 Strength (40–60)", rows[0].RevealedText);
        }

        [TestMethod]
        public void RowWithoutAnyRolledRangeExposesNullRevealedText()
        {
            var item = new EquipItem(EquipmentPiece.Ring, "Band", []);
            item.AddAdditionalModifier(new SimpleModifier(EntityParameter.Strength, ModifierValueType.Flat, 5f, "test"));

            var rows = EquipItemLines.ComposeRolled(item);

            Assert.IsNull(rows[0].RevealedText, "No range anywhere in the row — the caller falls back to Text.");
        }

        [TestMethod]
        public void AGatedRowNamesTheConditionThatHoldsItUp()
        {
            var item = new EquipItem(EquipmentPiece.Ring, "Band", []);
            item.AddAdditionalModifier(Gated(EntityParameter.Strength, TestConditions.Wounded));

            var rows = EquipItemLines.ComposeRolled(item);

            Assert.AreEqual("+5 Strength while wounded", rows[0].Text,
                "A line that only counts sometimes must say so — otherwise it reads as a bonus the player always has.");
        }

        [TestMethod]
        public void AGatedGroupNamesItsConditionOnceForTheWholeRow()
        {
            var item = new EquipItem(EquipmentPiece.Ring, "Band", []);
            item.AddAdditionalModifier(Gated(EntityParameter.Strength, TestConditions.Wounded, group: "g"));
            item.AddAdditionalModifier(Gated(EntityParameter.Intelligence, TestConditions.Wounded, group: "g"));

            var rows = EquipItemLines.ComposeRolled(item);

            Assert.AreEqual(1, rows.Count);
            Assert.AreEqual("+5 Strength, +5 Intelligence while wounded", rows[0].Text,
                "One composite is one player-facing line, so its gate is announced once.");
        }

        [TestMethod]
        public void TheAltRevealOfAGatedRowKeepsTheCondition()
        {
            var item = new EquipItem(EquipmentPiece.Ring, "Band", []);
            var rolled = new SimpleModifier(EntityParameter.Strength, ModifierValueType.Flat, 47f, "test")
            {
                ConditionId = TestConditions.Wounded, RolledRange = new ValueRange(40f, 60f),
            };
            item.AddAdditionalModifier(rolled);

            var rows = EquipItemLines.ComposeRolled(item);

            Assert.AreEqual("+47 Strength (40–60) while wounded", rows[0].RevealedText);
        }

        [TestMethod]
        public void AConditionTheCatalogHasNoWordingForShowsItsKey()
        {
            var item = new EquipItem(EquipmentPiece.Ring, "Band", []);
            item.AddAdditionalModifier(Gated(EntityParameter.Strength, "Health_Unworded"));

            var rows = EquipItemLines.ComposeRolled(item);

            Assert.AreEqual("+5 Strength Condition_Health_Unworded", rows[0].Text,
                "A missing string shows its key like everywhere else — it must not cost the gate.");
        }

        private static SimpleModifier Line(EntityParameter parameter, AffixKind affix) =>
            new(parameter, ModifierValueType.Flat, 5f, "test") { Affix = affix };

        private static SimpleModifier Gated(EntityParameter parameter, string conditionId, string? group = null) =>
            new(parameter, ModifierValueType.Flat, 5f, "test") { ConditionId = conditionId, GroupId = group };
    }
}

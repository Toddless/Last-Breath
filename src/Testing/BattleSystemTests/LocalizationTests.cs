namespace LastBreathTest.BattleSystemTests
{
    using Core.Data.GameData;
    using Core.Enums;
    using Core.Localization;
    using Core.Modifiers;

    internal sealed class FakeLocalizationProvider : ILocalizationProvider
    {
        public Dictionary<string, string> Strings { get; } = [];
        public Dictionary<string, (string One, string Many)> Plurals { get; } = [];

        public event Action? LocaleChanged;

        public string Locale
        {
            get;
            set
            {
                field = value;
                LocaleChanged?.Invoke();
            }
        } = "en";

        public string Translate(string key) => Strings.GetValueOrDefault(key, key);

        public string TranslatePlural(string singularKey, string pluralKey, int count) =>
            Plurals.TryGetValue(singularKey, out var forms)
                ? count == 1 ? forms.One : forms.Many
                : count == 1 ? singularKey : pluralKey;
    }

    [TestClass]
    public class TextTemplateEngineTests
    {
        private FakeLocalizationProvider _provider = null!;
        private TextTemplateEngine _engine = null!;

        [TestInitialize]
        public void Setup()
        {
            _provider = new FakeLocalizationProvider();
            _engine = new TextTemplateEngine(_provider);
        }

        [TestMethod]
        public void NamedPlaceholdersAreReplacedWithDotDecimalNumbers()
        {
            string result = _engine.Render("Deal {Damage} damage.", new Dictionary<string, object?> { ["Damage"] = 12.5f }, TextFormat.Plain);
            Assert.AreEqual("Deal 12.5 damage.", result);
        }

        [TestMethod]
        public void PositionalPlaceholdersStillWorkForLegacyTemplates()
        {
            string result = _engine.Render("Deal {0} hits, {1}% each.", new Dictionary<string, object?> { ["0"] = 6, ["1"] = 15f }, TextFormat.Plain);
            Assert.AreEqual("Deal 6 hits, 15% each.", result);
        }

        [TestMethod]
        public void UnknownPlaceholderStaysVisibleInsteadOfSilentHole()
        {
            string result = _engine.Render("Deal {Damage} damage.", new Dictionary<string, object?>(), TextFormat.Plain);
            Assert.AreEqual("Deal {Damage} damage.", result);
        }

        [TestMethod]
        public void EscapedBracesRenderLiterally()
        {
            string result = _engine.Render("Literal {{Damage}} and {Value}", new Dictionary<string, object?> { ["Value"] = 3 }, TextFormat.Plain);
            Assert.AreEqual("Literal {Damage} and 3", result);
        }

        [TestMethod]
        public void PluralTokenRendersCountAndLocalizedWord()
        {
            _provider.Plurals["turn"] = ("ход", "ходов");
            var one = _engine.Render("Lasts {Duration|turn|turns}.", new Dictionary<string, object?> { ["Duration"] = 1 }, TextFormat.Plain);
            var many = _engine.Render("Lasts {Duration|turn|turns}.", new Dictionary<string, object?> { ["Duration"] = 5 }, TextFormat.Plain);
            Assert.AreEqual("Lasts 1 ход.", one);
            Assert.AreEqual("Lasts 5 ходов.", many);
        }

        [TestMethod]
        public void PercentSuffixScalesFractionTimesHundredWithSign()
        {
            var values = new Dictionary<string, object?> { ["criticalChance"] = 0.15f };
            Assert.AreEqual("Gain 15% crit.", _engine.Render("Gain {criticalChance:%} crit.", values, TextFormat.Plain));
            Assert.AreEqual($"Gain {TextPalette.ColorizeNumber("15%")} crit.", _engine.Render("Gain {criticalChance:%} crit.", values, TextFormat.Rich));
        }

        [TestMethod]
        public void KeywordTokenRendersClickableLinkInRichAndPlainNameInPlain()
        {
            _provider.Strings["Effect_Clumsiness"] = "Clumsiness";
            var values = new Dictionary<string, object?>();
            Assert.AreEqual("Applies Clumsiness.", _engine.Render("Applies {@Effect_Clumsiness}.", values, TextFormat.Plain));
            Assert.AreEqual($"Applies [url=Effect_Clumsiness]{TextPalette.Colorize("Clumsiness", TextPalette.Keyword)}[/url].",
                _engine.Render("Applies {@Effect_Clumsiness}.", values, TextFormat.Rich));
        }

        [TestMethod]
        public void RichFormatColorsNumbersOnly()
        {
            string result = _engine.Render("Deal {Damage} {Kind} damage.", new Dictionary<string, object?> { ["Damage"] = 40, ["Kind"] = "fire" }, TextFormat.Rich);
            Assert.AreEqual($"Deal [color={TextPalette.Number}]40[/color] fire damage.", result);
        }
    }

    [TestClass]
    public class LocalizationKeywordProviderTests
    {
        private FakeLocalizationProvider _provider = null!;
        private LocalizationKeywordProvider _keywords = null!;

        [TestInitialize]
        public void Setup()
        {
            _provider = new FakeLocalizationProvider();
            _keywords = new LocalizationKeywordProvider(_provider);
        }

        [TestMethod]
        public void PrefersTooltipKeyOverDescription()
        {
            _provider.Strings["Effect_Fury"] = "Fury";
            _provider.Strings["Effect_Fury_Tooltip"] = "Static reference text.";
            _provider.Strings["Effect_Fury_Description"] = "Live {HealthPercent}% text.";

            Assert.IsTrue(_keywords.TryGetTooltip("Effect_Fury", out var view));
            Assert.AreEqual("Static reference text.", view.Description);
            Assert.AreEqual("Fury", view.Name);
        }

        [TestMethod]
        public void FallsBackToDescriptionWhenTooltipMissing()
        {
            _provider.Strings["Effect_X"] = "X";
            _provider.Strings["Effect_X_Description"] = "Description text.";

            Assert.IsTrue(_keywords.TryGetTooltip("Effect_X", out var view));
            Assert.AreEqual("Description text.", view.Description);
        }

        [TestMethod]
        public void UnknownKeyYieldsNoTooltip() =>
            Assert.IsFalse(_keywords.TryGetTooltip("Effect_Unknown", out _));
    }

    [TestClass]
    public class ModifierFormatterTests
    {
        private FakeLocalizationProvider _provider = null!;
        private ParameterFormatProvider _formats = null!;
        private ModifierFormatter _formatter = null!;

        [TestInitialize]
        public void Setup()
        {
            _provider = new FakeLocalizationProvider();
            _provider.Strings["Modifier_Flat"] = "{value} {parameter}";
            _provider.Strings["Modifier_Increase"] = "{value} increased {parameter}";
            _provider.Strings["Modifier_Multiplicative"] = "{value} more {parameter}";
            _provider.Strings["Modifier_Flat_Range"] = "{min}–{max} {parameter}";
            _provider.Strings["Modifier_Increase_Range"] = "{min}–{max} increased {parameter}";
            _provider.Strings["Modifier_Multiplicative_Range"] = "{min}–{max} more {parameter}";
            _provider.Strings["Damage"] = "Damage";
            _provider.Strings["CriticalChance"] = "Critical Chance";

            _formats = new ParameterFormatProvider();
            _formats.Apply(DataCatalog.Formatting, new GameDataFile("ParameterFormats.json",
                """{ "parameters": [ { "parameter": "CriticalChance", "unit": "Percent" } ] }"""));
            _formatter = new ModifierFormatter(_provider, _formats);
        }

        [TestMethod]
        public void FlatNumberParameterRendersPlainValue() =>
            Assert.AreEqual("+50 Damage", _formatter.Format(new Modifier(ModifierValueType.Flat, EntityParameter.Damage, 50f)));

        [TestMethod]
        public void FlatPercentParameterScalesFractionTimesHundred() =>
            // data stores fractions: 0.5 = 50%
            Assert.AreEqual("+50% Critical Chance", _formatter.Format(new Modifier(ModifierValueType.Flat, EntityParameter.CriticalChance, 0.5f)));

        [TestMethod]
        public void IncreaseRendersPercentDelta() =>
            Assert.AreEqual("+10% increased Damage", _formatter.Format(new Modifier(ModifierValueType.Increase, EntityParameter.Damage, 0.1f)));

        [TestMethod]
        public void MultiplicativeRendersDeltaAsPercent() =>
            // multiplicative values in data are DELTAS folded as (1 + Σ value): 0.2 = "+20% more"
            Assert.AreEqual("+20% more Damage", _formatter.Format(new Modifier(ModifierValueType.Multiplicative, EntityParameter.Damage, 0.2f)));

        [TestMethod]
        public void NegativeValuesCarryMinusSign() =>
            Assert.AreEqual("-15% increased Damage", _formatter.Format(new Modifier(ModifierValueType.Increase, EntityParameter.Damage, -0.15f)));

        [TestMethod]
        public void RangedFlatRendersThroughTheRangeTemplate() =>
            Assert.AreEqual("+40–60 Damage", _formatter.FormatRanged(new Modifier(ModifierValueType.Flat, EntityParameter.Damage, 50f), 0.8f, 1.2f));

        [TestMethod]
        public void RangedIncreaseCarriesUnitOnMaxOnly() =>
            Assert.AreEqual("+10–20% increased Damage", _formatter.FormatRanged(new Modifier(ModifierValueType.Increase, EntityParameter.Damage, 0.2f), 0.5f, 1f));

        [TestMethod]
        public void DescriptorWithSpreadRendersRangeTemplate() =>
            Assert.AreEqual("+40–60 Damage", _formatter.FormatDescriptor(
                new ParameterDescriptor(EntityParameter.Damage, ModifierValueType.Flat, new ValueRange(40f, 60f), ModifierScope.Global)));

        [TestMethod]
        public void DescriptorWithFixedValueRendersLikeALiveModifier() =>
            Assert.AreEqual("+50 Damage", _formatter.FormatDescriptor(
                new ParameterDescriptor(EntityParameter.Damage, ModifierValueType.Flat, 50f, ModifierScope.Global)));

        [TestMethod]
        public void DescriptorPercentParameterScalesBothBounds() =>
            Assert.AreEqual("+4–6% Critical Chance", _formatter.FormatDescriptor(
                new ParameterDescriptor(EntityParameter.CriticalChance, ModifierValueType.Flat, new ValueRange(0.04f, 0.06f), ModifierScope.Global)));

        [TestMethod]
        public void RolledRangeRendersBareIntervalScaledToTheCurrentValue()
        {
            // BaseValue 50 rolled from [40..60]; the item then upgraded the line to 100 (×2) —
            // the interval follows the value channel so bounds stay comparable with the shown number.
            var line = new SimpleModifier(EntityParameter.Damage, ModifierValueType.Flat, 50f, "test")
            {
                RolledRange = new ValueRange(40f, 60f),
                Value = 100f,
            };
            Assert.AreEqual("80–120", _formatter.FormatRolledRange(line));
        }

        [TestMethod]
        public void RolledRangeIsNullForFixedRolls() =>
            Assert.IsNull(_formatter.FormatRolledRange(new SimpleModifier(EntityParameter.Damage, ModifierValueType.Flat, 50f, "test")));

        [TestMethod]
        public void DecimalsUseDotRegardlessOfSystemCulture() =>
            Assert.AreEqual("+12.5 Damage", _formatter.Format(new Modifier(ModifierValueType.Flat, EntityParameter.Damage, 12.5f)));

        [TestMethod]
        public void RichFormatColorsTheValue() =>
            Assert.AreEqual($"[color={TextPalette.Number}]+50[/color] Damage",
                _formatter.Format(new Modifier(ModifierValueType.Flat, EntityParameter.Damage, 50f), TextFormat.Rich));

        [TestMethod]
        public void UnlistedParameterDefaultsToPlainNumber() =>
            Assert.AreEqual(ParameterUnit.Number, _formats.GetUnit(EntityParameter.Evade));

        [TestMethod]
        public void ParameterChangeAddScalesPercentParameterFraction() =>
            Assert.AreEqual("+5%", _formatter.FormatParameterChange(EntityParameter.CriticalChance, 0.05f, OperationType.Add));

        [TestMethod]
        public void ParameterChangeSubtractCarriesMinusSign() =>
            Assert.AreEqual("-50", _formatter.FormatParameterChange(EntityParameter.Damage, 50f, OperationType.Subtract));

        [TestMethod]
        public void ParameterChangeMultiplyShowsDeltaFromOne() =>
            Assert.AreEqual("+20%", _formatter.FormatParameterChange(EntityParameter.Damage, 1.2f, OperationType.Multiply));

        [TestMethod]
        public void ParameterChangeDivideShowsNegativeDelta() =>
            Assert.AreEqual("-50%", _formatter.FormatParameterChange(EntityParameter.Damage, 2f, OperationType.Divide));

        [TestMethod]
        public void ParameterChangeOverrideShowsAbsoluteValueWithoutSign() =>
            Assert.AreEqual("30", _formatter.FormatParameterChange(EntityParameter.Damage, 30f, OperationType.Override));
    }
}

namespace LastBreathTest.BattleSystemTests
{
    using System.Text.RegularExpressions;
    using Core.Enums;
    using Core.Localization;

    /// <summary>
    /// The damage-type keyword family: every type owns a color in the one palette, a name and a
    /// reference card in the catalog, and a {@Type} marker in a description renders as a link
    /// wearing that color — while the Effect_* keywords keep the shared accent they always had.
    /// </summary>
    [TestClass]
    public class DamageTypeKeywordTests
    {
        private static readonly Regex s_hexColor = new("^#[0-9A-Fa-f]{6}$", RegexOptions.Compiled);

        [TestMethod]
        public void EveryDamageTypeHasAColorOfItsOwn()
        {
            var seen = new Dictionary<string, DamageType>();
            foreach (DamageType type in Enum.GetValues<DamageType>())
            {
                string color = TextPalette.DamageColor(type);
                Assert.IsTrue(s_hexColor.IsMatch(color), $"{type} has no well-formed color: '{color}'");
                Assert.AreNotEqual("#ffffff", color, $"{type} fell through to the palette's fallback white — it is not listed");
                Assert.IsFalse(seen.TryGetValue(color, out DamageType other), $"{type} and {other} share the color {color}");
                seen[color] = type;
            }
        }

        [TestMethod]
        public void EveryDamageTypeIsWordedWithANameAndACardInBothCatalogs()
        {
            foreach (string po in new[] { "en.po", "ru.po" })
            {
                HashSet<string> ids = MsgIds(po);
                foreach (DamageType type in Enum.GetValues<DamageType>())
                {
                    string key = DamageKeywords.Key(type);
                    Assert.IsTrue(ids.Contains(key), $"{po} words no name for '{key}' — a {{@{key}}} link would echo its raw key");
                    Assert.IsTrue(ids.Contains(key + "_Tooltip"), $"{po} carries no card '{key}_Tooltip' — clicking the keyword would show nothing");
                }
            }
        }

        [TestMethod]
        public void DamageTypeKeywordRendersALinkWearingTheTypesColor()
        {
            var provider = new FakeLocalizationProvider();
            provider.Strings["Sacred"] = "Sacred";
            var engine = new TextTemplateEngine(provider);

            string rich = engine.Render("gains 1% as {@Sacred} damage.", new Dictionary<string, object?>(), TextFormat.Rich);
            Assert.AreEqual(
                $"gains 1% as [url=Sacred]{TextPalette.Colorize("Sacred", TextPalette.DamageColor(DamageType.Sacred))}[/url] damage.",
                rich);

            Assert.AreEqual("gains 1% as Sacred damage.",
                engine.Render("gains 1% as {@Sacred} damage.", new Dictionary<string, object?>(), TextFormat.Plain));
        }

        /// <summary>The battle log's DamageType_ spelling opens the same family — colored the same.</summary>
        [TestMethod]
        public void LogPrefixedKeywordCountsAsTheSameFamily()
        {
            var provider = new FakeLocalizationProvider();
            provider.Strings["DamageType_Fire"] = "Fire";

            string rich = new TextTemplateEngine(provider)
                .Render("{@DamageType_Fire} damage", new Dictionary<string, object?>(), TextFormat.Rich);

            Assert.AreEqual($"[url=DamageType_Fire]{TextPalette.Colorize("Fire", TextPalette.DamageColor(DamageType.Fire))}[/url] damage", rich);
        }

        [TestMethod]
        public void EffectKeywordsKeepTheSharedKeywordAccent()
        {
            var provider = new FakeLocalizationProvider();
            provider.Strings["Effect_Clumsiness"] = "Clumsiness";

            string rich = new TextTemplateEngine(provider)
                .Render("Applies {@Effect_Clumsiness}.", new Dictionary<string, object?>(), TextFormat.Rich);

            Assert.AreEqual($"Applies [url=Effect_Clumsiness]{TextPalette.Colorize("Clumsiness", TextPalette.Keyword)}[/url].", rich);
        }

        [TestMethod]
        public void DamageTypeCardCarriesItsTitleColorAndOthersCarryNone()
        {
            var provider = new FakeLocalizationProvider();
            provider.Strings["Fire"] = "Fire";
            provider.Strings["Fire_Tooltip"] = "Elemental fire damage.";
            provider.Strings["Effect_Fury"] = "Fury";
            provider.Strings["Effect_Fury_Tooltip"] = "Static reference text.";
            var keywords = new LocalizationKeywordProvider(provider);

            Assert.IsTrue(keywords.TryGetTooltip("Fire", out var fire));
            Assert.AreEqual(TextPalette.DamageColor(DamageType.Fire), fire.TitleColorHex);

            Assert.IsTrue(keywords.TryGetTooltip("Effect_Fury", out var fury));
            Assert.IsNull(fury.TitleColorHex, "an Effect_* card took a damage tint — the themed title color is lost");
        }

        [TestMethod]
        public void ParseAcceptsOnlyNamesOfTheFamily()
        {
            Assert.IsTrue(DamageKeywords.TryParse("Bleed", out DamageType bleed));
            Assert.AreEqual(DamageType.Bleed, bleed);
            Assert.IsTrue(DamageKeywords.TryParse("DamageType_Blight", out DamageType blight));
            Assert.AreEqual(DamageType.Blight, blight);

            Assert.IsFalse(DamageKeywords.TryParse("Effect_Fury", out _), "an effect key parsed as a damage type");
            Assert.IsFalse(DamageKeywords.TryParse("16", out _), "a numeric spelling parsed as a damage type");
            Assert.IsFalse(DamageKeywords.TryParse("fire", out _), "keys are case-exact; a lowercase word is prose, not a keyword");
        }

        /// <summary>msgids of a catalog, the same single-line convention the other audits read by.</summary>
        private static HashSet<string> MsgIds(string po)
        {
            HashSet<string> ids = [];
            foreach (string line in File.ReadAllLines(Path.Combine(SharedData.Root(), "Localization", po)))
                if (line.StartsWith("msgid \"", StringComparison.Ordinal) && line.EndsWith('"'))
                    ids.Add(line["msgid \"".Length..^1]);

            Assert.IsTrue(ids.Count > 0, $"{po} was not read at all, so this walk proves nothing");
            return ids;
        }
    }
}
